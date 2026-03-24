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

    // Formats that use -x/--audio-format and are post-processed by yt-dlp's own
    // audio pipeline; injecting -b:a via --postprocessor-args would conflict with
    // the format-specific quality handling (e.g. --audio-quality 0 for mp3).
    private static readonly HashSet<string> AudioOnlyFormats =
        new(StringComparer.OrdinalIgnoreCase) { "mp3", "wav", "flac", "ogg", "opus", "m4a" };

    public async Task DownloadAsync(
        DownloadOptions options,
        Action<DownloadProgress> onProgress,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = YtDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        foreach (var arg in BuildArguments(options))
            psi.ArgumentList.Add(arg);

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

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Kill the child process tree so yt-dlp and ffmpeg don't run orphaned
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            throw;
        }

        if (process.ExitCode != 0)
            throw new Exception($"yt-dlp exited with code {process.ExitCode}. Check the URL and try again.");

        onProgress(new DownloadProgress { Status = "Done!", Percent = 100, IsIndeterminate = false });
    }

    private static List<string> BuildArguments(DownloadOptions options)
    {
        var args       = new List<string>();
        var ffmpegArgs = new List<string>(); // merged into a single --postprocessor-args at the end

        switch (options.Format)
        {
            case "mp3":
                args.Add("-x"); args.Add("--audio-format"); args.Add("mp3");
                args.Add("--audio-quality"); args.Add("0");
                break;

            case "wav":
                args.Add("-x"); args.Add("--audio-format"); args.Add("wav");
                break;

            case "flac":
                args.Add("-x"); args.Add("--audio-format"); args.Add("flac");
                break;

            case "ogg":
                // yt-dlp uses "vorbis" as the codec name for ogg containers
                args.Add("-x"); args.Add("--audio-format"); args.Add("vorbis");
                break;

            case "opus":
                args.Add("-x"); args.Add("--audio-format"); args.Add("opus");
                break;

            case "m4a":
                args.Add("-x"); args.Add("--audio-format"); args.Add("m4a");
                break;

            case "avi":
                args.Add("-f"); args.Add(MapQualityToFilter(options.Quality));
                args.Add("--merge-output-format"); args.Add("avi");
                ffmpegArgs.Add("-c:v mpeg4 -c:a mp3");
                break;

            case "mkv":
                args.Add("-f"); args.Add(MapQualityToFilter(options.Quality));
                args.Add("--merge-output-format"); args.Add("mkv");
                break;

            case "webm":
                args.Add("-f"); args.Add(MapQualityToFilter(options.Quality));
                args.Add("--merge-output-format"); args.Add("webm");
                break;

            default: // mp4
                args.Add("-f"); args.Add(MapQualityToFilter(options.Quality));
                args.Add("--merge-output-format"); args.Add("mp4");
                ffmpegArgs.Add("-c:a aac -b:a 192k");
                break;
        }

        // Codec overrides (advanced mode)
        if (!string.IsNullOrEmpty(options.VideoCodec) && options.VideoCodec != "(Auto)")
            ffmpegArgs.Add($"-c:v {MapVideoCodec(options.VideoCodec)}");

        if (!string.IsNullOrEmpty(options.AudioBitrate) && !AudioOnlyFormats.Contains(options.Format))
            ffmpegArgs.Add($"-b:a {options.AudioBitrate}");

        // Emit all ffmpeg args as a single flag (passing it twice would silently ignore the second)
        if (ffmpegArgs.Count > 0)
        {
            args.Add("--postprocessor-args");
            args.Add($"ffmpeg:{string.Join(" ", ffmpegArgs)}");
        }

        args.Add("--ffmpeg-location"); args.Add(FfmpegPath);

        // Metadata embed
        if (options.EmbedMetadata)
            args.Add("--embed-metadata");

        // SponsorBlock — remove all category types yt-dlp supports
        if (options.RemoveSponsorBlock)
        {
            args.Add("--sponsorblock-remove"); args.Add("all");
        }

        // Browser cookies (for age-restricted / members-only content)
        if (!string.IsNullOrEmpty(options.CookiesFromBrowser) && options.CookiesFromBrowser != "(None)")
        {
            args.Add("--cookies-from-browser"); args.Add(options.CookiesFromBrowser);
        }

        // Playlist range
        if (options.PlaylistStart.HasValue)
        {
            args.Add("--playlist-start"); args.Add(options.PlaylistStart.Value.ToString());
        }
        if (options.PlaylistEnd.HasValue)
        {
            args.Add("--playlist-end"); args.Add(options.PlaylistEnd.Value.ToString());
        }

        // Thumbnails
        // Note: --embed-thumbnail for MP3 requires AtomicParsley.exe (bundled in Assets/)
        if (options.EmbedThumbnail)
            args.Add("--embed-thumbnail");
        if (options.WriteThumbnail)
            args.Add("--write-thumbnail");

        // Subtitles
        if (options.WriteSubtitles || options.EmbedSubtitles)
        {
            args.Add(options.EmbedSubtitles ? "--embed-subs" : "--write-subs");
            var subLangs = string.IsNullOrWhiteSpace(options.SubtitleLanguage) ? "en" : options.SubtitleLanguage;
            args.Add("--sub-langs"); args.Add(subLangs);
        }
        if (options.WriteAutoSubtitles)
            args.Add("--write-auto-subs");

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

        args.Add("-o"); args.Add(outputTemplate);
        args.Add("--newline");
        args.Add(options.Url);

        return args;
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

    private static string MapVideoCodec(string codec) => codec switch
    {
        "H.264"      => "libx264",
        "H.265/HEVC" => "libx265",
        "AV1"        => "libaom-av1",
        "VP9"        => "libvpx-vp9",
        _            => "copy",
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
            var psi = new ProcessStartInfo
            {
                FileName               = YtDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            psi.ArgumentList.Add("--dump-json");
            if (playlistFirstOnly)
            {
                psi.ArgumentList.Add("--playlist-items");
                psi.ArgumentList.Add("1");
            }
            else
            {
                psi.ArgumentList.Add("--no-playlist");
            }
            psi.ArgumentList.Add(url);

            using var process = Process.Start(psi)!;
            try
            {
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
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Gets the version string for yt-dlp, ffmpeg, or AtomicParsley.</summary>
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
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            psi.ArgumentList.Add(arg);

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
