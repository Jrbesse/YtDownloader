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
    private static readonly string AppDir =
        Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;

    public static string YtDlpPath  => Path.Combine(AppDir, "Assets", "yt-dlp.exe");
    public static string FfmpegPath => Path.Combine(AppDir, "Assets", "ffmpeg.exe");

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
            FileName               = YtDlpPath,
            Arguments              = args,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            onProgress(ParseProgress(e.Data));
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            onProgress(new DownloadProgress
            {
                Status          = "Processing…",
                Detail          = e.Data,
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

        switch (options.Format)
        {
            case "mp3":
                parts.Add("-x --audio-format mp3 --audio-quality 0");
                break;
            case "webm":
                parts.Add("-f bestvideo[ext=webm]+bestaudio[ext=webm]/best[ext=webm]");
                break;
            default:
                var qualityFilter = MapQualityToFilter(options.Quality);
                parts.Add($"-f \"{qualityFilter}\"");
                break;
        }

        parts.Add($"--ffmpeg-location \"{FfmpegPath}\"");

        var outputTemplate = options.IsPlaylist
            ? Path.Combine(options.OutputFolder, "%(playlist)s", "%(playlist_index)s - %(title)s.%(ext)s")
            : Path.Combine(options.OutputFolder, "%(title)s.%(ext)s");

        parts.Add($"-o \"{outputTemplate}\"");
        parts.Add("--newline");
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
        _            => "bestvideo+bestaudio/best",
    };

    private static DownloadProgress ParseProgress(string line)
    {
        if (line.StartsWith("[ffmpeg]") || line.StartsWith("[Merger]"))
        {
            return new DownloadProgress
            {
                Status          = "Merging streams…",
                Detail          = "ffmpeg is combining video and audio",
                IsIndeterminate = true,
            };
        }

        var match = ProgressRegex.Match(line);
        if (match.Success)
        {
            var pct = double.Parse(match.Groups["pct"].Value);
            return new DownloadProgress
            {
                Status          = "Downloading…",
                Detail          = $"{match.Groups["size"].Value}  ·  {match.Groups["speed"].Value}  ·  ETA {match.Groups["eta"].Value}",
                Percent         = pct,
                IsIndeterminate = false,
            };
        }

        if (line.Contains("[download] Destination:"))
            return new DownloadProgress
            {
                Status          = "Starting download…",
                Detail          = line.Replace("[download] Destination:", "").Trim(),
                IsIndeterminate = true,
            };

        if (line.Contains("[info]") || line.Contains("[youtube]"))
            return new DownloadProgress
            {
                Status          = "Fetching video info…",
                Detail          = line,
                IsIndeterminate = true,
            };

        return new DownloadProgress
        {
            Status          = "Working…",
            Detail          = line,
            IsIndeterminate = true,
        };
    }

    /// <summary>
    /// Fetches video metadata without downloading.
    /// Pass playlistFirstOnly: true for playlist URLs to get the first entry's info.
    /// </summary>
    public static async Task<VideoInfo?> FetchVideoInfoAsync(
        string url,
        CancellationToken ct = default,
        bool playlistFirstOnly = false)
    {
        try
        {
            // --playlist-items 1 fetches only the first entry from a playlist
            var playlistArg = playlistFirstOnly ? "--playlist-items 1" : "--no-playlist";

            var psi = new ProcessStartInfo
            {
                FileName               = YtDlpPath,
                Arguments              = $"--dump-json {playlistArg} \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            using var process = Process.Start(psi)!;
            // For playlists, yt-dlp outputs one JSON object per line — read only the first
            var json = await process.StandardOutput.ReadLineAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(json))
                return null;

            using var doc  = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new VideoInfo
            {
                Title           = root.TryGetProperty("title",     out var t)  ? t.GetString()  ?? "" : "",
                Channel         = root.TryGetProperty("channel",   out var c)  ? c.GetString()  ?? "" : "",
                ThumbnailUrl    = root.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? "" : "",
                DurationSeconds = root.TryGetProperty("duration",  out var d)  ? d.GetDouble()       : 0,
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
                FileName               = path,
                Arguments              = arg,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            using var p = Process.Start(psi)!;
            var output = await p.StandardOutput.ReadLineAsync();
            await p.WaitForExitAsync();

            if (output?.Split(' ').Length > 3)
                output = output.Split(' ')[2];

            return output?.Trim();
        }
        catch
        {
            return null;
        }
    }
}
