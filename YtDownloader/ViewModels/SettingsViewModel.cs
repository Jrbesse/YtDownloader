using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _ytDlpVersion  = "Checking…";
    [ObservableProperty] private string _ffmpegVersion = "Checking…";
    [ObservableProperty] private string _atomicParsleyVersion = "Checking…";
    [ObservableProperty] private string _updateStatus  = string.Empty;
    [ObservableProperty] private bool   _isUpdating    = false;

    // ── Pass-throughs to AppSettings singleton ────────────────────────────────

    public bool ShowDiagnostics
    {
        get => AppSettings.Instance.ShowDiagnostics;
        set => AppSettings.Instance.ShowDiagnostics = value;
    }

    public bool ShowNotifications
    {
        get => AppSettings.Instance.ShowNotifications;
        set { AppSettings.Instance.ShowNotifications = value; OnPropertyChanged(); }
    }

    public bool AutoCheckUpdates
    {
        get => AppSettings.Instance.AutoCheckUpdates;
        set { AppSettings.Instance.AutoCheckUpdates = value; OnPropertyChanged(); }
    }

    public bool RememberOutputFolder
    {
        get => AppSettings.Instance.RememberOutputFolder;
        set { AppSettings.Instance.RememberOutputFolder = value; OnPropertyChanged(); }
    }

    public bool IsAdvancedMode
    {
        get => AppSettings.Instance.IsAdvancedMode;
        set
        {
            if (AppSettings.Instance.IsAdvancedMode == value) return;
            AppSettings.Instance.IsAdvancedMode = value;
            OnPropertyChanged();
            AdvancedModeChanged?.Invoke(value);
        }
    }

    /// <summary>Fired when the user toggles Advanced Mode so MainWindow can react.</summary>
    public static event Action<bool>? AdvancedModeChanged;

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

    // ── Dependency versions ───────────────────────────────────────────────────

    public async Task LoadVersionsAsync()
    {
        YtDlpVersion           = await YtDlpService.GetVersionAsync("yt-dlp")  ?? "Not found";
        FfmpegVersion          = await YtDlpService.GetVersionAsync("ffmpeg")  ?? "Not found";
        AtomicParsleyVersion   = await YtDlpService.GetVersionAsync("AtomicParsley") ?? "Not found";
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
}
