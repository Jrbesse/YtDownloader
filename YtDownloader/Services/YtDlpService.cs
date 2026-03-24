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

    public static string YtDlpPath         => Path.Combine(AppDir, "Assets", "yt-dlp.exe");
    public static string FfmpegPath        => Path.Combine(AppDir, "Assets", "ffmpeg.exe");
    public static string AtomicParsleyPath => Path.Combine(AppDir, "Assets", "AtomicParsley.exe");

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

    /// <summary>
    /// Builds the yt-dlp command-line argument string from the given download options.
    /// </summary>
    /// <param name="options">DownloadOptions that control format, post-processing, metadata, subtitles, thumbnails, cookies, playlist range, output template, and the target URL.</param>
    /// <returns>The complete command-line argument string to pass to the yt-dlp executable.</returns>
    private static string BuildArguments(DownloadOptions options)
    {
        var parts      = new List<string>();
        var ffmpegArgs = new List<string>(); // merged into a single --postprocessor-args at the end

        switch (options.Format)
        {
            case "mp3":
                parts.Add("-x --audio-format mp3 --audio-quality 0");
                break;

            case "wav":
                parts.Add("-x --audio-format wav");
                break;

            case "flac":
                parts.Add("-x --audio-format flac");
                break;

            case "ogg":
                // yt-dlp uses "vorbis" as the codec name for ogg containers
                parts.Add("-x --audio-format vorbis");
                break;

            case "opus":
                parts.Add("-x --audio-format opus");
                break;

            case "m4a":
                parts.Add("-x --audio-format m4a");
                break;

            case "avi":
                parts.Add($"-f \"{MapQualityToFilter(options.Quality)}\"");
                parts.Add("--merge-output-format avi");
                ffmpegArgs.Add("-c:v mpeg4 -c:a mp3 -b:a 192k");
                break;

            case "mkv":
                parts.Add($"-f \"{MapQualityToFilter(options.Quality)}\"");
                parts.Add("--merge-output-format mkv");
                break;

            case "webm":
                parts.Add($"-f \"{MapQualityToFilter(options.Quality)}\"");
                parts.Add("--merge-output-format webm");
                break;

            default: // mp4
                parts.Add($"-f \"{MapQualityToFilter(options.Quality)}\"");
                parts.Add("--merge-output-format mp4");
                ffmpegArgs.Add("-c:a aac -b:a 192k");
                break;
        }

        // Codec overrides (advanced mode)
        if (!string.IsNullOrEmpty(options.VideoCodec) && options.VideoCodec != "(Auto)")
            ffmpegArgs.Add($"-c:v {MapVideoCodec(options.VideoCodec)}");

        if (!string.IsNullOrEmpty(options.AudioBitrate))
            ffmpegArgs.Add($"-b:a {options.AudioBitrate}");

        // Emit all ffmpeg args as a single flag (passing it twice would silently ignore the second)
        if (ffmpegArgs.Count > 0)
            parts.Add($"--postprocessor-args \"ffmpeg:{string.Join(" ", ffmpegArgs)}\"");

        parts.Add($"--ffmpeg-location \"{FfmpegPath}\"");

        // Metadata embed
        if (options.EmbedMetadata)
            parts.Add("--embed-metadata");

        // SponsorBlock — remove all category types yt-dlp supports
        if (options.RemoveSponsorBlock)
            parts.Add("--sponsorblock-remove all");

        // Browser cookies (for age-restricted / members-only content)
        if (!string.IsNullOrEmpty(options.CookiesFromBrowser) && options.CookiesFromBrowser != "(None)")
            parts.Add($"--cookies-from-browser {options.CookiesFromBrowser}");

        // Playlist range
        if (options.PlaylistStart.HasValue)
            parts.Add($"--playlist-start {options.PlaylistStart.Value}");
        if (options.PlaylistEnd.HasValue)
            parts.Add($"--playlist-end {options.PlaylistEnd.Value}");

        // Thumbnails
        // Note: --embed-thumbnail for MP3 requires AtomicParsley.exe (bundled in Assets/)
        if (options.EmbedThumbnail)
            parts.Add("--embed-thumbnail");
        if (options.WriteThumbnail)
            parts.Add("--write-thumbnail");

        // Subtitles
        if (options.WriteSubtitles || options.EmbedSubtitles)
        {
            parts.Add(options.EmbedSubtitles ? "--embed-subs" : "--write-subs");
            parts.Add($"--sub-langs {options.SubtitleLanguage}");
        }
        if (options.WriteAutoSubtitles)
            parts.Add("--write-auto-subs");

        // Output template
        string outputTemplate;
        if (!string.IsNullOrEmpty(options.CustomOutputTemplate))
        {
            // If the user gave a relative template, prepend the output folder
            outputTemplate = Path.IsPathRooted(options.CustomOutputTemplate)
                ? options.CustomOutputTemplate
                : Path.Combine(options.OutputFolder, options.CustomOutputTemplate);
        }
        else if (options.IsPlaylist)
        {
            outputTemplate = Path.Combine(options.OutputFolder,
                "%(playlist)s", "%(playlist_index)s - %(title)s.%(ext)s");
        }
        else
        {
            outputTemplate = Path.Combine(options.OutputFolder, "%(title)s.%(ext)s");
        }

        parts.Add($"-o \"{outputTemplate}\"");
        parts.Add("--newline");
        parts.Add($"\"{options.Url}\"");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Map a human-readable quality label to the corresponding yt-dlp format selection expression.
    /// </summary>
    /// <param name="quality">Quality label chosen by the user (e.g., "2160p (4K)", "1080p", "720p").</param>
    /// <returns>The yt-dlp format selection expression that selects the requested video+audio streams or a sensible default when the label is unrecognized.</returns>
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

    /// <summary>
    /// Map a UI codec label to the corresponding ffmpeg encoder name used in post-processing.
    /// </summary>
    /// <param name="codec">Codec label presented in the UI (for example "H.264", "H.265/HEVC", "AV1", "VP9").</param>
    /// <returns>The ffmpeg encoder name for the given label (e.g. "libx264"), or "copy" to preserve streams when no mapping exists.</returns>
    private static string MapVideoCodec(string codec) => codec switch
    {
        "H.264"      => "libx264",
        "H.265/HEVC" => "libx265",
        "AV1"        => "libaom-av1",
        "VP9"        => "libvpx-vp9",
        _            => "copy",
    };

    /// <summary>
    /// Parses a single yt-dlp stdout/stderr line and converts it into a DownloadProgress describing the current operation.
    /// </summary>
    /// <param name="line">A single output line produced by yt-dlp or related tools (for example ffmpeg or merger).</param>
    /// <returns>A DownloadProgress containing a human-readable status, detail text, `Percent` when available, and whether the progress is indeterminate.</returns>
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
    /// <summary>
    /// Retrieves basic metadata for a video (or the first item of a playlist) by invoking yt-dlp and parsing its JSON output.
    /// </summary>
    /// <param name="url">The video or playlist URL to query.</param>
    /// <param name="ct">Cancellation token used to cancel the underlying process and read operations.</param>
    /// <param name="playlistFirstOnly">If true and the URL is a playlist, only the first playlist item is queried; otherwise the URL is treated as a single video.</param>
    /// <returns>A <see cref="VideoInfo"/> containing title, channel, thumbnail URL, and duration in seconds, or <c>null</c> if information cannot be retrieved.</returns>
    public static async Task<VideoInfo?> FetchVideoInfoAsync(
        string url,
        CancellationToken ct = default,
        bool playlistFirstOnly = false)
    {
        try
        {
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
            var json = await process.StandardOutput.ReadLineAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(json))
                return null;

            using var doc = System.Text.Json.JsonDocument.Parse(json);
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

    /// <summary>
    /// Retrieve the installed version string for the specified tool.
    /// </summary>
    /// <param name="tool">The tool to query: use "yt-dlp" or "AtomicParsley"; any other value queries ffmpeg.</param>
    /// <returns>The tool's version token (for example "2024.12.23" or "7.1"), the literal "Not found" if the executable is missing, or `null` if an error occurs while querying.</returns>
    public static async Task<string?> GetVersionAsync(string tool)
    {
        try
        {
            string path, arg;
            if (tool == "yt-dlp")
            {
                path = YtDlpPath;
                arg  = "--version";
            }
            else if (tool == "AtomicParsley")
            {
                path = AtomicParsleyPath;
                arg  = "--version";
            }
            else // ffmpeg
            {
                path = FfmpegPath;
                arg  = "-version";
            }

            if (!File.Exists(path)) return "Not found";

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

            // ffmpeg:        "ffmpeg version 7.1 Copyright ..." → index [2]
            // AtomicParsley: "AtomicParsley version: 0.9.6 ..." → index [2]
            // yt-dlp:        "2024.12.23"                       → keep as-is
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
