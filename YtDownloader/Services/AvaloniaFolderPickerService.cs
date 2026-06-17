using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using YtDownloader.Services;

namespace YtDownloader;

internal sealed class AvaloniaFolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select output folder", AllowMultiple = false });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private static TopLevel? GetTopLevel() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
