using System.Diagnostics;
using System.Text.RegularExpressions;
using YtDownloader.Models;

namespace YtDownloader.Services;

/// <summary>
/// Wraps yt-dlp.exe as a managed subprocess, parsing its stdout for
/// real-time progress updates. ffmpeg is invoked automatically by yt-dlp
/// for post-processing (muxing, audio extraction, etc.).
/// </summary>
public class YtDlpService
{
    // Paths to bundled binaries (copied next to the .exe at publish time)
    private static readonly string AppDir =
        Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;

    public static string YtDlpPath => Path.Combine(AppDir, "Assets", "yt-dlp.exe");
    public static string FfmpegPath => Path.Combine(AppDir, "Assets", "ffmpeg.exe");

    // Regex to parse yt-dlp's "[download]  45.3% of  847.12MiB at  9.10MiB/s ETA 00:38"
    private static readonly Regex ProgressRegex = new(
        @"\[download\]\s+(?<pct>[\d.]+)%\s+of\s+(?<size>[\d.]+\S+)\s+at\s+(?<speed>[\d.]+\S+)\s+ETA\s+(?<eta>\S+)",
        RegexOptions.Compiled);

    public async Task DownloadAsync(
        DownloadOptions options,
        Action<DownloadProgress> onProgress,
        CancellationToken ct = default)
    {
        var args = BuildArguments(options);
        var psi = new ProcessStartInfo
        {
            FileName = YtDlpPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            var progress = ParseProgress(e.Data);
            onProgress(progress);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            onProgress(new DownloadProgress
            {
                Status = "Processing…",
                Detail = e.Data,
                IsIndeterminate = true,
            });
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new Exception($"yt-dlp exited with code {process.ExitCode}. Check the URL and try again.");

        onProgress(new DownloadProgress { Status = "Done!", Percent = 100, IsIndeterminate = false });
    }

    private static string BuildArguments(DownloadOptions options)
    {
        var parts = new List<string>();

        // Format selection
        switch (options.Format)
        {
            case "mp3":
                parts.Add("-x --audio-format mp3 --audio-quality 0");
                break;
            case "webm":
                parts.Add("-f bestvideo[ext=webm]+bestaudio[ext=webm]/best[ext=webm]");
                break;
            default: // mp4
                var qualityFilter = MapQualityToFilter(options.Quality);
                parts.Add($"-f \"{qualityFilter}\"");
                break;
        }

        // ffmpeg location
        parts.Add($"--ffmpeg-location \"{FfmpegPath}\"");

        // Output template
        var outputTemplate = options.IsPlaylist
            ? Path.Combine(options.OutputFolder, "%(playlist)s", "%(playlist_index)s - %(title)s.%(ext)s")
            : Path.Combine(options.OutputFolder, "%(title)s.%(ext)s");

        parts.Add($"-o \"{outputTemplate}\"");

        // Progress in a machine-parseable format
        parts.Add("--newline");

        // The URL (always last)
        parts.Add($"\"{options.Url}\"");

        return string.Join(" ", parts);
    }

    private static string MapQualityToFilter(string quality) => quality switch
    {
        "2160p (4K)" => "bestvideo[height<=2160]+bestaudio/best[height<=2160]",
        "1440p"      => "bestvideo[height<=1440]+bestaudio/best[height<=1440]",
        "1080p"      => "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
        "720p"       => "bestvideo[height<=720]+bestaudio/best[height<=720]",
        "480p"       => "bestvideo[height<=480]+bestaudio/best[height<=480]",
        "360p"       => "bestvideo[height<=360]+bestaudio/best[height<=360]",
        _            => "bestvideo+bestaudio/best",   // "Best available"
    };

    private static DownloadProgress ParseProgress(string line)
    {
        // Downloading a fragment / merging
        if (line.StartsWith("[ffmpeg]") || line.StartsWith("[Merger]"))
        {
            return new DownloadProgress
            {
                Status = "Merging streams…",
                Detail = "ffmpeg is combining video and audio",
                IsIndeterminate = true,
            };
        }

        var match = ProgressRegex.Match(line);
        if (match.Success)
        {
            var pct = double.Parse(match.Groups["pct"].Value);
            return new DownloadProgress
            {
                Status = "Downloading…",
                Detail = $"{match.Groups["size"].Value}  ·  {match.Groups["speed"].Value}  ·  ETA {match.Groups["eta"].Value}",
                Percent = pct,
                IsIndeterminate = false,
            };
        }

        if (line.Contains("[download] Destination:"))
        {
            return new DownloadProgress
            {
                Status = "Starting download…",
                Detail = line.Replace("[download] Destination:", "").Trim(),
                IsIndeterminate = true,
            };
        }

        if (line.Contains("[info]") || line.Contains("[youtube]"))
        {
            return new DownloadProgress
            {
                Status = "Fetching video info…",
                Detail = line,
                IsIndeterminate = true,
            };
        }

        return new DownloadProgress
        {
            Status = "Working…",
            Detail = line,
            IsIndeterminate = true,
        };
    }

    /// <summary>
    /// Runs yt-dlp --dump-json to fetch video metadata without downloading.
    /// Returns null if the URL is invalid or the fetch fails.
    /// </summary>
    public static async Task<VideoInfo?> FetchVideoInfoAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = YtDlpPath,
                Arguments = $"--dump-json --no-playlist \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi)!;
            var json = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(json))
                return null;

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new VideoInfo
            {
                Title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Channel = root.TryGetProperty("channel", out var c) ? c.GetString() ?? "" : "",
                ThumbnailUrl = root.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? "" : "",
                DurationSeconds = root.TryGetProperty("duration", out var d) ? d.GetDouble() : 0,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Gets the version string for yt-dlp or ffmpeg.</summary>
    public static async Task<string?> GetVersionAsync(string tool)
    {
        try
        {
            var path = tool == "yt-dlp" ? YtDlpPath : FfmpegPath;
            var arg  = tool == "yt-dlp" ? "--version" : "-version";
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arg,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi)!;
            var output = await p.StandardOutput.ReadLineAsync();
            await p.WaitForExitAsync();
            return output?.Trim();
        }
        catch
        {
            return null;
        }
    }
}
