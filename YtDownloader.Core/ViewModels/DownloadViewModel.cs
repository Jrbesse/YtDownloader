using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class DownloadViewModel : ObservableObject
{
    private readonly YtDlpService _ytDlp = new();
    private readonly HistoryService _history = HistoryService.Instance;
    private readonly INotificationService _notifications;
    private readonly IFileRevealService _fileReveal;
    private readonly IFolderPickerService _folderPicker;
    private readonly IClipboardService _clipboard;

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

    [ObservableProperty] private bool _isVideoInfoVisible;
    [ObservableProperty] private bool _isPreviewLoadingVisible;
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

        IsVideoInfoVisible      = false;
        IsPreviewLoadingVisible = false;

        if (string.IsNullOrWhiteSpace(url)) return;
        if (!url.Contains("youtube.com/") && !url.Contains("youtu.be/")) return;

        try
        {
            await Task.Delay(PreviewDebounceMs, ct);
            IsPreviewLoadingVisible = true;

            // For playlists pass --playlist-items 1 so we only fetch the first entry
            var info = await YtDlpService.FetchVideoInfoAsync(url, ct, playlistFirstOnly: IsPlaylist);
            if (ct.IsCancellationRequested) return;

            IsPreviewLoadingVisible = false;
            if (info is null) return;

            VideoTitle    = info.Title;
            ChannelName   = info.Channel;
            VideoDuration = info.DurationFormatted;
            ThumbnailUrl  = info.ThumbnailUrl;
            PreviewLabel  = IsPlaylist ? "First video in playlist" : "Video preview";

            IsVideoInfoVisible = true;
        }
        catch (TaskCanceledException) { IsPreviewLoadingVisible = false; }
        catch                         { IsPreviewLoadingVisible = false; }
    }

    // ── Format ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQualityVisible))]
    private bool _isMp4Selected = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQualityVisible))]
    private bool _isAviSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQualityVisible))]
    private bool _isMp3Selected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQualityVisible))]
    private bool _isWavSelected;

    // Hide quality selector for audio-only formats
    public bool IsQualityVisible => !(IsMp3Selected || IsWavSelected);

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

    [ObservableProperty] private bool _isProgressVisible;
    [ObservableProperty] private bool _isCancelVisible;
    [ObservableProperty] private bool _isDoneVisible;
    [ObservableProperty] private string _progressStatus  = string.Empty;
    [ObservableProperty] private string _progressPercent = string.Empty;
    [ObservableProperty] private string _progressDetail  = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool   _isProgressIndeterminate;
    [ObservableProperty] private string _doneMessage = string.Empty;
    [ObservableProperty] private bool _isDownloadVisible = true;

    // ── yt-dlp update state ───────────────────────────────────────────────────

    [ObservableProperty] private bool _isUpdateBannerVisible = false;
    [ObservableProperty] private string _updateBannerText = string.Empty;

    // ── Verbose log ───────────────────────────────────────────────────────────

    [ObservableProperty] private string _logText      = string.Empty;
    [ObservableProperty] private bool   _isLogVisible;

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PasteFromClipboard()
    {
        var text = await _clipboard.GetTextAsync();
        if (text is not null)
            Url = text;
    }

    [RelayCommand]
    private async Task BrowseFolder()
    {
        var folder = await _folderPicker.PickFolderAsync();
        if (folder is not null)
            OutputFolder = folder;
    }

    private bool CanDownload() => !YtDlpUpdateState.IsSwappingFile;

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task Download()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;

        _downloadCts = new CancellationTokenSource();

        IsDoneVisible           = false;
        IsProgressVisible       = true;
        IsCancelVisible         = true;
        IsDownloadVisible       = false;
        IsProgressIndeterminate = true;
        ProgressStatus          = "Fetching video info…";
        ProgressDetail          = "Connecting to YouTube…";
        ProgressValue           = 0;
        LogText                 = string.Empty;
        IsLogVisible            = AppSettings.Instance.VerboseLogging;

        try
        {
            var isAudio = SelectedFormat is "mp3" or "wav";
            var options = new DownloadOptions
            {
                Url           = Url,
                Format        = SelectedFormat,
                Quality       = SelectedQuality,
                OutputFolder  = OutputFolder,
                IsPlaylist    = IsPlaylist,
                EmbedMetadata = isAudio,
            };

            await _ytDlp.DownloadAsync(options, OnProgress, _downloadCts.Token);

            IsCancelVisible   = true;
            IsDownloadVisible = false;
            IsProgressVisible = false;
            IsDoneVisible     = true;
            DoneMessage       = $"Saved to {OutputFolder}";

            var completedTitle = string.IsNullOrEmpty(VideoTitle) ? "Download" : VideoTitle;
            _notifications.SendDownloadComplete(completedTitle, OutputFolder);

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
            IsCancelVisible   = true;
            IsDownloadVisible = false;
            IsProgressVisible = false;
            ProgressStatus    = string.Empty;
            ProgressDetail    = string.Empty;
            ProgressValue     = 0;
        }
        catch (Exception ex)
        {
            IsCancelVisible         = true;
            IsDownloadVisible       = false;
            IsProgressVisible       = true;
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
                IsLogVisible  = true;
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
    private void OpenFolder() => _fileReveal.RevealInFileManager(OutputFolder);

    [RelayCommand]
    private void Reset()
    {
        Url                = string.Empty;
        VideoTitle         = string.Empty;
        ChannelName        = string.Empty;
        VideoDuration      = string.Empty;
        ThumbnailUrl       = string.Empty;
        PreviewLabel       = "Video preview";
        IsVideoInfoVisible = false;
        IsProgressVisible  = false;
        IsCancelVisible    = false;
        IsDownloadVisible  = true;
        IsDoneVisible      = false;
        ProgressValue      = 0;
    }

    // ── Progress callback ─────────────────────────────────────────────────────

    private void OnProgress(DownloadProgress progress)
    {
        Dispatcher.UIThread.Post(() =>
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

    public DownloadViewModel(
        INotificationService notifications,
        IFileRevealService fileReveal,
        IFolderPickerService folderPicker,
        IClipboardService clipboard)
    {
        _notifications = notifications;
        _fileReveal    = fileReveal;
        _folderPicker  = folderPicker;
        _clipboard     = clipboard;

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
        Dispatcher.UIThread.Post(() =>
        {
            UpdateBannerText = YtDlpUpdateState.IsSwappingFile
                ? "Applying yt-dlp update, please wait…"
                : "Downloading yt-dlp update in the background…";

            IsUpdateBannerVisible = YtDlpUpdateState.IsUpdating || YtDlpUpdateState.IsSwappingFile;
            DownloadCommand.NotifyCanExecuteChanged();
        });
    }
}
