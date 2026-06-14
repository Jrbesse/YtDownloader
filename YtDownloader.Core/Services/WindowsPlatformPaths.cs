namespace YtDownloader.Services;

/// <summary>
/// Windows path layout: settings/history live under %LOCALAPPDATA%\YtDownloader,
/// and tool binaries (yt-dlp.exe/ffmpeg.exe/ffprobe.exe) live alongside the app
/// in an "Assets" folder — matching the existing bundled-executable layout.
/// </summary>
public sealed class WindowsPlatformPaths : IPlatformPaths
{
    public string ConfigDir => AppDataRoot;

    public string DataDir => AppDataRoot;

    public string ToolsDir => Path.Combine(AppContext.BaseDirectory, "Assets");

    public string ExecutableExtension => ".exe";

    private static string AppDataRoot
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(localAppData))
                localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? AppContext.BaseDirectory;

            return Path.Combine(localAppData, "YtDownloader");
        }
    }
}
