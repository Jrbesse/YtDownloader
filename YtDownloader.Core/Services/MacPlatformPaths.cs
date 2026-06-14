namespace YtDownloader.Services;

/// <summary>
/// macOS path layout: settings/history live under
/// ~/Library/Application Support/YtDownloader, and tool binaries are cached
/// under ~/Library/Caches/YtDownloader/tools (downloaded on first run).
/// </summary>
public sealed class MacPlatformPaths : IPlatformPaths
{
    public string ConfigDir => AppSupportDir;

    public string DataDir => AppSupportDir;

    public string ToolsDir => Path.Combine(Home, "Library", "Caches", "YtDownloader", "tools");

    public string ExecutableExtension => string.Empty;

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static string AppSupportDir => Path.Combine(Home, "Library", "Application Support", "YtDownloader");
}
