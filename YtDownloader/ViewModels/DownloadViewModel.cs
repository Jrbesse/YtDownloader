using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class DownloadViewModel : ObservableObject
{
    private readonly YtDlpService _ytDlp = new();
    private readonly HistoryService _history = HistoryService.Instance;
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private CancellationTokenSource? _previewCts;
    private CancellationTokenSource? _downloadCts;
    private const int PreviewDebounceMs = 800;

    // ── URL & Detection ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlaylist))]
    [NotifyPropertyChangedFor(nameof(PlaylistMessage))]
    private string _url = string.Empty;

    partial void OnUrlChanged(string value) => _ = FetchPreviewDebounced(value);

    public bool IsPlaylist => Url.Contains("playlist") || Url.Contains("list=");

    public string PlaylistMessage => IsPlaylist
        ? "All videos in this playlist will be downloaded to a subfolder."
        : string.Empty;

    // ── Video Preview ────────────────────────────────────────────────────────

    [ObservableProperty] private Visibility _videoInfoVisibility     = Visibility.Collapsed;
    [ObservableProperty] private Visibility _previewLoadingVisibility = Visibility.Collapsed;
    [ObservableProperty] private string _videoTitle    = string.Empty;
    [ObservableProperty] private string _channelName   = string.Empty;
    [ObservableProperty] private string _videoDuration = string.Empty;
    [ObservableProperty] private string _thumbnailUrl  = string.Empty;

    // Label shown above the preview card — changes for playlists
    [ObservableProperty] private string _previewLabel = "Video preview";

    private async Task FetchPreviewDebounced(string url)
    {
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var ct = _previewCts.Token;

        VideoInfoVisibility      = Visibility.Collapsed;
        PreviewLoadingVisibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(url)) return;
        if (!url.Contains("youtube.com/") && !url.Contains("youtu.be/")) return;

        try
        {
            await Task.Delay(PreviewDebounceMs, ct);
            PreviewLoadingVisibility = Visibility.Visible;

            // For playlists pass --playlist-items 1 so we only fetch the first entry
            var info = await YtDlpService.FetchVideoInfoAsync(url, ct, playlistFirstOnly: IsPlaylist);
            if (ct.IsCancellationRequested) return;

            PreviewLoadingVisibility = Visibility.Collapsed;
            if (info is null) return;

            VideoTitle    = info.Title;
            ChannelName   = info.Channel;
            VideoDuration = info.DurationFormatted;
            ThumbnailUrl  = info.ThumbnailUrl;
            PreviewLabel  = IsPlaylist ? "First video in playlist" : "Video preview";

            VideoInfoVisibility = Visibility.Visible;
        }
        catch (TaskCanceledException) { PreviewLoadingVisibility = Visibility.Collapsed; }
        catch                         { PreviewLoadingVisibility = Visibility.Collapsed; }
    }

    // ── Format ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private bool _isMp4Selected = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private bool _isAviSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private bool _isMp3Selected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private bool _isWavSelected;

    // Hide quality selector for audio-only formats
    public Visibility QualityVisibility =>
        (IsMp3Selected || IsWavSelected) ? Visibility.Collapsed : Visibility.Visible;

    public string SelectedFormat =>
        IsMp3Selected ? "mp3" :
        IsAviSelected ? "avi" :
        IsWavSelected ? "wav" : "mp4";

    // ── Quality ──────────────────────────────────────────────────────────────

    public ObservableCollection<string> AvailableQualities { get; } = new()
    {
        "Best available", "2160p (4K)", "1440p", "1080p", "720p", "480p", "360p"
    };

    [ObservableProperty] private string _selectedQuality = "Best available";

    // ── Output Folder ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _outputFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    partial void OnOutputFolderChanged(string value)
    {
        if (AppSettings.Instance.RememberOutputFolder)
            AppSettings.Instance.LastOutputFolder = value;
    }

    // ── Progress & Download State ─────────────────────────────────────────────

    [ObservableProperty] private Visibility _progressVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility _cancelVisibility   = Visibility.Collapsed;
    [ObservableProperty] private Visibility _doneVisibility     = Visibility.Collapsed;
    [ObservableProperty] private string _progressStatus  = string.Empty;
    [ObservableProperty] private string _progressPercent = string.Empty;
    [ObservableProperty] private string _progressDetail  = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool   _isProgressIndeterminate;
    [ObservableProperty] private string _doneMessage = string.Empty;
    [ObservableProperty] private Visibility _downloadVisibility = Visibility.Visible;

    // ── yt-dlp update state ───────────────────────────────────────────────────

    [ObservableProperty] private bool _updateBannerVisibility = false;
    [ObservableProperty] private string _updateBannerText = string.Empty;

    // ── Verbose log ───────────────────────────────────────────────────────────

    [ObservableProperty] private string     _logText       = string.Empty;
    [ObservableProperty] private Visibility _logVisibility = Visibility.Collapsed;

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PasteFromClipboard()
    {
        var package = Clipboard.GetContent();
        if (package.Contains(StandardDataFormats.Text))
            Url = await package.GetTextAsync();
    }

    [RelayCommand]
    private async Task BrowseFolder()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.Downloads;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
            OutputFolder = folder.Path;
    }

    private bool CanDownload() => !YtDlpUpdateState.IsSwappingFile;

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task Download()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;

        _downloadCts = new CancellationTokenSource();

        DoneVisibility          = Visibility.Collapsed;
        ProgressVisibility      = Visibility.Visible;
        CancelVisibility        = Visibility.Visible;
        DownloadVisibility      = Visibility.Collapsed;
        IsProgressIndeterminate = true;
        ProgressStatus          = "Fetching video info…";
        ProgressDetail          = "Connecting to YouTube…";
        ProgressValue           = 0;
        LogText                 = string.Empty;
        LogVisibility           = AppSettings.Instance.VerboseLogging ? Visibility.Visible : Visibility.Collapsed;

        try
        {
            var isAudio = SelectedFormat is "mp3" or "wav";
            var options = new DownloadOptions
            {
                Url                = Url,
                Format             = SelectedFormat,
                Quality            = SelectedQuality,
                OutputFolder       = OutputFolder,
                IsPlaylist         = IsPlaylist,
                EmbedMetadata      = isAudio,
                CookiesFromBrowser = BrowserDetectionService.GetDefaultBrowserForYtDlp(),
            };

            await _ytDlp.DownloadAsync(options, OnProgress, _downloadCts.Token);

            CancelVisibility   = Visibility.Visible;
            DownloadVisibility = Visibility.Collapsed;
            ProgressVisibility = Visibility.Collapsed;
            DoneVisibility     = Visibility.Visible;
            DoneMessage        = $"Saved to {OutputFolder}";

            var completedTitle = string.IsNullOrEmpty(VideoTitle) ? "Download" : VideoTitle;
            NotificationService.SendDownloadComplete(completedTitle, OutputFolder);

            _history.Add(new DownloadHistoryItem
            {
                Title       = string.IsNullOrEmpty(VideoTitle) ? "Download" : VideoTitle,
                Url         = Url,
                OutputPath  = OutputFolder,
                Format      = SelectedFormat.ToUpper(),
                Quality     = SelectedQuality,
                CompletedAt = DateTime.Now,
            });
        }
        catch (OperationCanceledException)
        {
            CancelVisibility   = Visibility.Visible;
            DownloadVisibility = Visibility.Collapsed;
            ProgressVisibility = Visibility.Collapsed;
            ProgressStatus     = string.Empty;
            ProgressDetail     = string.Empty;
            ProgressValue      = 0;
        }
        catch (Exception ex)
        {
            CancelVisibility        = Visibility.Visible;
            DownloadVisibility      = Visibility.Collapsed;
            ProgressVisibility      = Visibility.Visible;
            IsProgressIndeterminate = false;
            ProgressValue           = 0;
            ProgressPercent         = string.Empty;
            ProgressStatus          = "Couldn't complete the download.";
            ProgressDetail          = "Check your connection and try the link again. " +
                                      "Still having trouble? Try Advanced Mode in Settings — " +
                                      "it has extra options that can help.";
            if (AppSettings.Instance.VerboseLogging)
            {
                LogText      += $"[error]\n{ex}\n";
                LogVisibility = Visibility.Visible;
            }
        }
        finally
        {
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    [RelayCommand]
    private void CancelDownload() => _downloadCts?.Cancel();

    [RelayCommand]
    private void OpenFolder() =>
        Process.Start("explorer.exe", $"\"{OutputFolder}\"");

    [RelayCommand]
    private void Reset()
    {
        Url                  = string.Empty;
        VideoTitle           = string.Empty;
        ChannelName          = string.Empty;
        VideoDuration        = string.Empty;
        ThumbnailUrl         = string.Empty;
        PreviewLabel         = "Video preview";
        VideoInfoVisibility  = Visibility.Collapsed;
        ProgressVisibility   = Visibility.Collapsed;
        CancelVisibility     = Visibility.Visible;
        DownloadVisibility   = Visibility.Collapsed;
        DoneVisibility       = Visibility.Collapsed;
        ProgressValue        = 0;
    }

    // ── Progress callback ─────────────────────────────────────────────────────

    private void OnProgress(DownloadProgress progress)
    {
        _dispatcher.TryEnqueue(() =>
        {
            IsProgressIndeterminate = progress.IsIndeterminate;
            ProgressStatus  = progress.Status;
            ProgressDetail  = progress.Detail;
            ProgressValue   = progress.Percent;
            ProgressPercent = progress.IsIndeterminate ? "" : $"{progress.Percent:0}%";

            if (AppSettings.Instance.VerboseLogging && !string.IsNullOrEmpty(progress.Detail))
                LogText += progress.Detail + "\n";
        });
    }

    public DownloadViewModel()
    {
        // Restore last used folder if the setting is enabled
        if (AppSettings.Instance.RememberOutputFolder
            && !string.IsNullOrEmpty(AppSettings.Instance.LastOutputFolder))
        {
            _outputFolder = AppSettings.Instance.LastOutputFolder;
        }

        YtDlpUpdateState.StateChanged += OnUpdateStateChanged;
    }

    private void OnUpdateStateChanged()
    {
        _dispatcher.TryEnqueue(() =>
        {
            UpdateBannerText = YtDlpUpdateState.IsSwappingFile
                ? "Applying yt-dlp update, please wait…"
                : "Downloading yt-dlp update in the background…";

            UpdateBannerVisibility = YtDlpUpdateState.IsUpdating || YtDlpUpdateState.IsSwappingFile;
            DownloadCommand.NotifyCanExecuteChanged();
        });
    }
}
