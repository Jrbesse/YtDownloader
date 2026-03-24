using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage.Pickers;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class AdvancedViewModel : ObservableObject
{
    private readonly YtDlpService _ytDlp      = new();
    private readonly HistoryService _history  = HistoryService.Instance;
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private CancellationTokenSource? _queueCts;

    // ── Static option lists ───────────────────────────────────────────────────

    public List<string> AvailableFormats { get; } = new()
    {
        "mp4", "mkv", "webm", "avi", "mp3", "wav", "flac", "ogg", "opus", "m4a"
    };

    public ObservableCollection<string> AvailableQualities { get; } = new()
    {
        "Best available", "2160p (4K)", "1440p", "1080p", "720p", "480p", "360p"
    };

    public List<string> VideoCodecs { get; } = new()
    {
        "(Auto)", "H.264", "H.265/HEVC", "AV1", "VP9"
    };

    public List<string> AudioBitrates { get; } = new()
    {
        "128k", "192k", "320k"
    };

    public List<string> Browsers { get; } = new()
    {
        "(None)", "chrome", "firefox", "edge", "brave"
    };

    // ── Format & quality ──────────────────────────────────────────────────────

    private static readonly HashSet<string> VideoFormats = new() { "mp4", "mkv", "webm", "avi" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoCodecVisibility))]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private string _selectedFormat = "mp4";

    [ObservableProperty] private string _selectedQuality = "Best available";

    public Visibility VideoCodecVisibility => VideoFormats.Contains(SelectedFormat)
        ? Visibility.Visible : Visibility.Collapsed;

    public Visibility QualityVisibility => VideoFormats.Contains(SelectedFormat)
        ? Visibility.Visible : Visibility.Collapsed;

    // ── Codec & bitrate ───────────────────────────────────────────────────────

    [ObservableProperty] private string _selectedVideoCodec   = "(Auto)";
    [ObservableProperty] private string _selectedAudioBitrate = "192k";

    // ── URLs ──────────────────────────────────────────────────────────────────

    [ObservableProperty] private string _urlsText = string.Empty;

    // ── Options ───────────────────────────────────────────────────────────────

    [ObservableProperty] private bool   _enableSponsorBlock   = false;
    [ObservableProperty] private string _selectedBrowser      = "(None)";
    [ObservableProperty] private string _playlistStartText    = string.Empty;
    [ObservableProperty] private string _playlistEndText      = string.Empty;
    [ObservableProperty] private bool   _embedThumbnail       = false;
    [ObservableProperty] private bool   _writeThumbnail       = false;
    [ObservableProperty] private bool   _writeSubtitles       = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubtitleOptionsVisibility))]
    private bool _subtitlesEnabled = false;

    [ObservableProperty] private bool   _embedSubtitles       = false;
    [ObservableProperty] private bool   _writeAutoSubtitles   = false;
    [ObservableProperty] private string _subtitleLanguage     = "en";
    [ObservableProperty] private bool   _embedMetadata        = true;

    public Visibility SubtitleOptionsVisibility => SubtitlesEnabled
        ? Visibility.Visible : Visibility.Collapsed;

    // ── Output ────────────────────────────────────────────────────────────────

    [ObservableProperty] private string _outputTemplate = "%(title)s.%(ext)s";

    [ObservableProperty]
    private string _outputFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    // ── Queue ─────────────────────────────────────────────────────────────────

    public ObservableCollection<AdvancedDownloadQueueItem> Queue { get; } = new();

    [ObservableProperty] private bool _isQueueRunning = false;
    [ObservableProperty] private Visibility _stopVisibility = Visibility.Collapsed;

    // ── Verbose log ───────────────────────────────────────────────────────────

    [ObservableProperty] private string     _logText       = string.Empty;
    [ObservableProperty] private Visibility _logVisibility = Visibility.Collapsed;

    public AdvancedViewModel()
    {
        if (AppSettings.Instance.RememberOutputFolder
            && !string.IsNullOrEmpty(AppSettings.Instance.LastOutputFolder))
        {
            _outputFolder = AppSettings.Instance.LastOutputFolder;
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

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
        {
            OutputFolder = folder.Path;
            if (AppSettings.Instance.RememberOutputFolder)
                AppSettings.Instance.LastOutputFolder = OutputFolder;
        }
    }

    [RelayCommand]
    private void OpenFolder() =>
        Process.Start("explorer.exe", $"\"{OutputFolder}\"");

    [RelayCommand(CanExecute = nameof(CanStartQueue))]
    private async Task StartQueue()
    {
        var urls = UrlsText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();

        if (urls.Count == 0) return;

        Queue.Clear();
        foreach (var url in urls)
            Queue.Add(new AdvancedDownloadQueueItem { Url = url });

        _queueCts      = new CancellationTokenSource();
        IsQueueRunning = true;
        StopVisibility = Visibility.Visible;
        LogText        = string.Empty;
        LogVisibility  = AppSettings.Instance.VerboseLogging ? Visibility.Visible : Visibility.Collapsed;
        StartQueueCommand.NotifyCanExecuteChanged();

        foreach (var item in Queue)
        {
            if (_queueCts.Token.IsCancellationRequested)
            {
                item.Status = QueueItemStatus.Cancelled;
                continue;
            }
            await ProcessQueueItem(item, _queueCts.Token);
        }

        IsQueueRunning = false;
        StopVisibility = Visibility.Collapsed;
        StartQueueCommand.NotifyCanExecuteChanged();
        _queueCts?.Dispose();
        _queueCts = null;
    }

    private bool CanStartQueue() => !IsQueueRunning;

    [RelayCommand]
    private void StopQueue() => _queueCts?.Cancel();

    // ── Queue processing ──────────────────────────────────────────────────────

    private async Task ProcessQueueItem(AdvancedDownloadQueueItem item, CancellationToken ct)
    {
        item.Status          = QueueItemStatus.Downloading;
        item.IsIndeterminate = true;
        item.Detail          = string.Empty;

        try
        {
            // Parse playlist range
            int? playlistStart = int.TryParse(PlaylistStartText, out var ps) ? ps : null;
            int? playlistEnd   = int.TryParse(PlaylistEndText,   out var pe) ? pe : null;

            var options = new DownloadOptions
            {
                Url                  = item.Url,
                Format               = SelectedFormat,
                Quality              = SelectedQuality,
                OutputFolder         = OutputFolder,
                EmbedMetadata        = EmbedMetadata,
                VideoCodec           = SelectedVideoCodec == "(Auto)" ? null : SelectedVideoCodec,
                AudioBitrate         = VideoFormats.Contains(SelectedFormat) ? SelectedAudioBitrate : null,
                RemoveSponsorBlock   = EnableSponsorBlock,
                CookiesFromBrowser   = SelectedBrowser == "(None)" ? null : SelectedBrowser,
                PlaylistStart        = playlistStart,
                PlaylistEnd          = playlistEnd,
                EmbedThumbnail       = EmbedThumbnail,
                WriteThumbnail       = WriteThumbnail,
                WriteSubtitles       = SubtitlesEnabled && !EmbedSubtitles,
                EmbedSubtitles       = SubtitlesEnabled && EmbedSubtitles,
                WriteAutoSubtitles   = SubtitlesEnabled && WriteAutoSubtitles,
                SubtitleLanguage     = SubtitleLanguage,
                CustomOutputTemplate = string.IsNullOrWhiteSpace(OutputTemplate)
                                       || OutputTemplate == "%(title)s.%(ext)s"
                                           ? null
                                           : OutputTemplate,
            };

            await _ytDlp.DownloadAsync(options, progress => UpdateItemProgress(item, progress), ct);

            item.Status          = QueueItemStatus.Done;
            item.Percent         = 100;
            item.IsIndeterminate = false;
            item.Detail          = OutputFolder;

            NotificationService.SendDownloadComplete(item.Url, OutputFolder);

            _history.Add(new DownloadHistoryItem
            {
                Title       = item.Url,
                Url         = item.Url,
                OutputPath  = OutputFolder,
                Format      = SelectedFormat.ToUpper(),
                Quality     = SelectedQuality,
                CompletedAt = DateTime.Now,
            });
        }
        catch (OperationCanceledException)
        {
            item.Status = QueueItemStatus.Cancelled;
        }
        catch (Exception ex)
        {
            item.Status = QueueItemStatus.Failed;
            item.Detail = ex.Message;
        }
    }

    private void UpdateItemProgress(AdvancedDownloadQueueItem item, DownloadProgress progress)
    {
        _dispatcher.TryEnqueue(() =>
        {
            item.IsIndeterminate = progress.IsIndeterminate;
            item.Percent         = progress.Percent;
            item.Detail          = progress.Detail;

            if (AppSettings.Instance.VerboseLogging && !string.IsNullOrEmpty(progress.Detail))
                LogText += progress.Detail + "\n";
        });
    }
}
