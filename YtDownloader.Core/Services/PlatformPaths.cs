namespace YtDownloader.Services;

/// <summary>
/// Provides the active <see cref="IPlatformPaths"/> for the current OS.
/// Defaults to a platform-appropriate implementation; tests may substitute
/// their own via the setter and restore with <see cref="ResetToDefault"/>.
/// </summary>
public static class PlatformPaths
{
    private static IPlatformPaths? _current;

    public static IPlatformPaths Current
    {
        get => _current ??= CreateDefault();
        set => _current = value;
    }

    public static void ResetToDefault() => _current = null;

    private static IPlatformPaths CreateDefault()
    {
        if (OperatingSystem.IsWindows()) return new WindowsPlatformPaths();
        if (OperatingSystem.IsMacOS()) return new MacPlatformPaths();
        return new LinuxPlatformPaths();
    }
}
