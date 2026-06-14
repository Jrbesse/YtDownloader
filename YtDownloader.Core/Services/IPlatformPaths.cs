namespace YtDownloader.Services;

/// <summary>
/// Abstracts the platform-specific filesystem locations YtDownloader uses for
/// configuration (settings.json), user data (history.json), and cached tool
/// binaries (yt-dlp/ffmpeg/ffprobe).
/// </summary>
public interface IPlatformPaths
{
    /// <summary>Directory that holds settings.json.</summary>
    string ConfigDir { get; }

    /// <summary>Directory that holds history.json and other user data.</summary>
    string DataDir { get; }

    /// <summary>Directory where yt-dlp/ffmpeg/ffprobe binaries are located.</summary>
    string ToolsDir { get; }

    /// <summary>File extension appended to tool executable names (".exe" on Windows, "" elsewhere).</summary>
    string ExecutableExtension { get; }
}
