using YtDownloader.Services;

namespace YtDownloader.Core.Tests;

/// <summary>Test double for <see cref="INotificationService"/> that records calls.</summary>
internal sealed class FakeNotificationService : INotificationService
{
    public List<(string Title, string Folder)> Notifications { get; } = new();

    public void SendDownloadComplete(string title, string folder) => Notifications.Add((title, folder));
}
