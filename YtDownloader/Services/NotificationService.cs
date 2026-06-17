using System.Diagnostics;
using YtDownloader.Services;

namespace YtDownloader;

internal sealed class NotificationService : INotificationService
{
    public void SendDownloadComplete(string title, string folder)
    {
        if (!AppSettings.Instance.ShowNotifications) return;

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "osascript",
                    ArgumentList    = { "-e", $"display notification \"{Escape(folder)}\" with title \"Download complete\" subtitle \"{Escape(title)}\"" },
                    UseShellExecute = false,
                    CreateNoWindow  = true,
                });
            }
            // Windows: deferred to packaging phase (requires Microsoft.WindowsAppSDK).
            // Linux:   no notifications in v1.
        }
        catch { }
    }

    private static string Escape(string s) => s.Replace("\"", "\\\"");
}
