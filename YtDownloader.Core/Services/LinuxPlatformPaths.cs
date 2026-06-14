namespace YtDownloader.Services;

/// <summary>
/// Linux path layout: follows the XDG Base Directory specification.
/// Settings live under $XDG_CONFIG_HOME/YtDownloader (default ~/.config),
/// history under $XDG_DATA_HOME/YtDownloader (default ~/.local/share), and
/// tool binaries are cached under $XDG_CACHE_HOME/YtDownloader/tools
/// (default ~/.cache, downloaded on first run).
/// </summary>
public sealed class LinuxPlatformPaths : IPlatformPaths
{
    public string ConfigDir => GetXdgDir("XDG_CONFIG_HOME", ".config");

    public string DataDir => GetXdgDir("XDG_DATA_HOME", Path.Combine(".local", "share"));

    public string ToolsDir => Path.Combine(GetXdgDir("XDG_CACHE_HOME", ".cache"), "tools");

    public string ExecutableExtension => string.Empty;

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static string GetXdgDir(string envVar, string fallbackRelativeToHome)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        var baseDir = string.IsNullOrEmpty(value) ? Path.Combine(Home, fallbackRelativeToHome) : value;
        return Path.Combine(baseDir, "YtDownloader");
    }
}
