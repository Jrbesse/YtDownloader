namespace YtDownloader.Services;

/// <summary>
/// Shows a system notification when a download completes. Implementations
/// are expected to honor <see cref="AppSettings.ShowNotifications"/> and to
/// degrade silently if the platform notification API is unavailable.
/// </summary>
public interface INotificationService
{
    /// <summary>Notifies the user that a download finished.</summary>
    /// <param name="title">The title of the downloaded item.</param>
    /// <param name="folder">The folder the download was saved to.</param>
    void SendDownloadComplete(string title, string folder);
}
