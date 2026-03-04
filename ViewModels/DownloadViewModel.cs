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

    // Debounce: wait this long after the user stops typing before fetching info
    private CancellationTokenSource? _previewCts;
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

    [ObservableProperty] private Visibility _videoInfoVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility _previewLoadingVisibility = Visibility.Collapsed;
    [ObservableProperty] private string _videoTitle = string.Empty;
    [ObservableProperty] private string _channelName = string.Empty;
    [ObservableProperty] private string _videoDuration = string.Empty;

    // ThumbnailUrl is set as a string; DownloadPage.xaml.cs converts it to BitmapImage
    [ObservableProperty] private string _thumbnailUrl = string.Empty;

    //private async Task FetchPreviewDebounced(string url)
    //{
    //    // Cancel any in-flight fetch
    //    _previewCts?.Cancel();
    //    _previewCts = new CancellationTokenSource();
    //    var ct = _previewCts.Token;

    //    // Hide old preview immediately
    //    VideoInfoVisibility = Visibility.Collapsed;
    //    PreviewLoadingVisibility = Visibility.Collapsed;

    //    if (string.IsNullOrWhiteSpace(url) || IsPlaylist) return;

    //    // Simple sanity check — must look like a YouTube URL
    //    if (!url.Contains("youtube.com/") && !url.Contains("youtu.be/")) return;

    //    try
    //    {
    //        // Debounce: wait for user to stop typing
    //        await Task.Delay(PreviewDebounceMs, ct);

    //        // Show spinner
    //        PreviewLoadingVisibility = Visibility.Visible;

    //        var info = await YtDlpService.FetchVideoInfoAsync(url, ct);

    //        if (ct.IsCancellationRequested) return;

    //        PreviewLoadingVisibility = Visibility.Collapsed;

    //        if (info is null) return;

    //        VideoTitle    = info.Title;
    //        ChannelName   = info.Channel;
    //        VideoDuration = info.DurationFormatted;
    //        ThumbnailUrl  = info.ThumbnailUrl;

    //        VideoInfoVisibility = Visibility.Visible;
    //    }
    //    catch (TaskCanceledException)
    //    {
    //        PreviewLoadingVisibility = Visibility.Collapsed;
    //    }
    //    catch
    //    {
    //        PreviewLoadingVisibility = Visibility.Collapsed;
    //    }
    //}

    private async Task FetchPreviewDebounced(string url)
    {
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var ct = _previewCts.Token;

        VideoInfoVisibility = Visibility.Collapsed;
        PreviewLoadingVisibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(url) || IsPlaylist)
        {
            //System.Diagnostics.Debug.WriteLine("[Preview] Skipped: empty or playlist");
            return;
        }

        if (!url.Contains("youtube.com/") && !url.Contains("youtu.be/"))
        {
            //System.Diagnostics.Debug.WriteLine($"[Preview] Skipped: URL didn't pass sanity check: '{url}'");
            return;
        }

        try
        {
            await Task.Delay(PreviewDebounceMs, ct);

            PreviewLoadingVisibility = Visibility.Visible;
            //System.Diagnostics.Debug.WriteLine($"[Preview] Fetching info for: {url}");

            var info = await YtDlpService.FetchVideoInfoAsync(url, ct);

            //System.Diagnostics.Debug.WriteLine($"[Preview] Result: {(info is null ? "NULL" : $"Title='{info.Title}', Thumb='{info.ThumbnailUrl}'")}");

            if (ct.IsCancellationRequested) return;

            PreviewLoadingVisibility = Visibility.Collapsed;

            if (info is null) return;

            VideoTitle = info.Title;
            ChannelName = info.Channel;
            VideoDuration = info.DurationFormatted;
            ThumbnailUrl = info.ThumbnailUrl;

            VideoInfoVisibility = Visibility.Visible;
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[Preview] Cancelled");
            PreviewLoadingVisibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Preview] Exception: {ex.Message}");
            PreviewLoadingVisibility = Visibility.Collapsed;
        }
    }

    // ── Format ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private bool _isMp4Selected = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private bool _isMp3Selected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private bool _isWebMSelected;

    public Visibility QualityVisibility => IsMp3Selected ? Visibility.Collapsed : Visibility.Visible;

    public string SelectedFormat => IsMp3Selected ? "mp3" : IsWebMSelected ? "webm" : "mp4";

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

    // ── Progress State ────────────────────────────────────────────────────────

    [ObservableProperty] private Visibility _progressVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility _doneVisibility = Visibility.Collapsed;
    [ObservableProperty] private string _progressStatus = string.Empty;
    [ObservableProperty] private string _progressPercent = string.Empty;
    [ObservableProperty] private string _progressDetail = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private string _doneMessage = string.Empty;

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

    [RelayCommand]
    private async Task Download()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;

        DoneVisibility         = Visibility.Collapsed;
        ProgressVisibility     = Visibility.Visible;
        IsProgressIndeterminate = true;
        ProgressStatus         = "Fetching video info…";
        ProgressDetail         = "Connecting to YouTube…";
        ProgressValue          = 0;

        try
        {
            var options = new DownloadOptions
            {
                Url          = Url,
                Format       = SelectedFormat,
                Quality      = SelectedQuality,
                OutputFolder = OutputFolder,
                IsPlaylist   = IsPlaylist,
            };

            await _ytDlp.DownloadAsync(options, OnProgress);

            ProgressVisibility = Visibility.Collapsed;
            DoneVisibility     = Visibility.Visible;
            DoneMessage        = $"Saved to {OutputFolder}";

            _history.Add(new DownloadHistoryItem
            {
                Title       = string.IsNullOrEmpty(VideoTitle) ? "Download" : VideoTitle,
                OutputPath  = OutputFolder,
                Format      = SelectedFormat.ToUpper(),
                Quality     = SelectedQuality,
                CompletedAt = DateTime.Now,
            });
        }
        catch (Exception ex)
        {
            ProgressVisibility = Visibility.Collapsed;
            ProgressStatus     = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenFolder() =>
        Process.Start("explorer.exe", $"\"{OutputFolder}\"");

    [RelayCommand]
    private void Reset()
    {
        Url                 = string.Empty;
        VideoTitle          = string.Empty;
        ChannelName         = string.Empty;
        VideoDuration       = string.Empty;
        ThumbnailUrl        = string.Empty;
        VideoInfoVisibility = Visibility.Collapsed;
        ProgressVisibility  = Visibility.Collapsed;
        DoneVisibility      = Visibility.Collapsed;
        ProgressValue       = 0;
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
        });
    }
}
