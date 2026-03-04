using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage.Pickers;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _ytDlpVersion  = "Checking…";
    [ObservableProperty] private string _ffmpegVersion = "Checking…";
    [ObservableProperty] private string _updateStatus  = string.Empty;
    [ObservableProperty] private bool   _isUpdating    = false;

    [ObservableProperty] private bool _showNotifications    = true;
    [ObservableProperty] private bool _autoCheckUpdates     = true;
    [ObservableProperty] private bool _rememberOutputFolder = true;

    // ── Pass-through to AppSettings singleton ─────────────────────────────────

    public bool ShowDiagnostics
    {
        get => AppSettings.Instance.ShowDiagnostics;
        set => AppSettings.Instance.ShowDiagnostics = value;
    }

    // Theme: bound to a ComboBox with items "System", "Light", "Dark"
    public string SelectedTheme
    {
        get => AppSettings.Instance.Theme;
        set
        {
            if (AppSettings.Instance.Theme == value) return;
            AppSettings.Instance.Theme = value;
            OnPropertyChanged();
        }
    }

    public List<string> AvailableThemes { get; } = new() { "System", "Light", "Dark" };

    // ── Default folder ────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _defaultFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    // ── Dependency versions ───────────────────────────────────────────────────

    public async void LoadVersionsAsync()
    {
        YtDlpVersion  = await YtDlpService.GetVersionAsync("yt-dlp")  ?? "Not found";
        FfmpegVersion = await YtDlpService.GetVersionAsync("ffmpeg") ?? "Not found";
    }

    [RelayCommand]
    private async Task CheckUpdates()
    {
        IsUpdating   = true;
        UpdateStatus = "Checking…";

        YtDlpUpdaterService.StatusChanged += OnUpdateStatus;
        await YtDlpUpdaterService.CheckAndUpdateAsync();
        YtDlpUpdaterService.StatusChanged -= OnUpdateStatus;

        YtDlpVersion = await YtDlpService.GetVersionAsync("yt-dlp") ?? "Not found";
        IsUpdating   = false;
    }

    private void OnUpdateStatus(string message) => UpdateStatus = message;

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
