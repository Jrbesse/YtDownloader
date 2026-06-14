namespace YtDownloader.Services;

/// <summary>
/// Lightweight shared state so the UI can react to background update activity.
/// </summary>
public static class YtDlpUpdateState
{
    // True while the yt-dlp binary is being downloaded or swapped
    public static bool IsUpdating { get; private set; }

    // True only during the brief file swap — Download should be blocked
    public static bool IsSwappingFile { get; private set; }

    // Simple notification — ViewModels subscribe to this
    public static event Action? StateChanged;

    internal static void SetUpdating(bool value)
    {
        IsUpdating = value;
        StateChanged?.Invoke();
    }

    internal static void SetSwappingFile(bool value)
    {
        IsSwappingFile = value;
        StateChanged?.Invoke();
    }
}