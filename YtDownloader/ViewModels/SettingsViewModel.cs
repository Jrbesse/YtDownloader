using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage.Pickers;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _ytDlpVersion = "Checking…";
    [ObservableProperty] private string _ffmpegVersion = "Checking…";

    [ObservableProperty] private bool _showNotifications = true;
    [ObservableProperty] private bool _autoCheckUpdates = true;
    [ObservableProperty] private bool _rememberOutputFolder = true;

    [ObservableProperty]
    private string _defaultFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    public async void LoadVersionsAsync()
    {
        YtDlpVersion = await YtDlpService.GetVersionAsync("yt-dlp") ?? "Not found";
        FfmpegVersion = await YtDlpService.GetVersionAsync("ffmpeg") ?? "Not found";
    }

    [RelayCommand]
    private void CheckUpdates()
    {
        LoadVersionsAsync();
    }

    [RelayCommand]
    private async Task BrowseDefaultFolder()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.Downloads;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
            DefaultFolder = folder.Path;
    }
}
