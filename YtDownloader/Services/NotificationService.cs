using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace YtDownloader.Services;

/// <summary>
/// Sends Windows toast notifications on download completion.
/// Requires notification registration in App.OnLaunched before use.
/// Unpackaged apps may not support notifications in all environments;
/// all calls are wrapped in try/catch to degrade gracefully.
/// </summary>
public static class NotificationService
{
    private static bool _registered;

    /// <summary>
    /// Registers the app for toast notifications. Call once in App.OnLaunched
    /// before MainWindow.Activate().
    /// </summary>
    public static void Register()
    {
        if (_registered) return;
        try
        {
            AppNotificationManager.Default.Register();
            _registered = true;
        }
        catch
        {
            // Registration can fail in unpackaged dev builds — notifications
            // are optional so we degrade gracefully.
        }
    }

    /// <summary>
    /// Shows a toast notification for a completed download if notifications
    /// are enabled in AppSettings and registration succeeded.
    /// </summary>
    public static void SendDownloadComplete(string title, string folder)
    {
        if (!_registered) return;
        if (!AppSettings.Instance.ShowNotifications) return;

        try
        {
            var notification = new AppNotificationBuilder()
                .AddText("Download complete")
                .AddText(title)
                .AddText(folder)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Never crash the app over a notification failure
        }
    }
}
