using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using YtDownloader.Services;

namespace YtDownloader;

internal sealed class AvaloniaClipboardService : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        var topLevel = GetTopLevel();
        return topLevel?.Clipboard is { } clipboard ? await clipboard.TryGetTextAsync() : null;
    }

    private static TopLevel? GetTopLevel() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
