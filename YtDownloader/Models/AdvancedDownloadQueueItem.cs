using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDownloader.Models;

public enum QueueItemStatus { Pending, Downloading, Done, Failed, Cancelled }

public partial class AdvancedDownloadQueueItem : ObservableObject
{
    public string Url { get; set; } = string.Empty;

    [ObservableProperty] private QueueItemStatus _status      = QueueItemStatus.Pending;
    [ObservableProperty] private string          _statusText  = "Pending";
    [ObservableProperty] private double          _percent     = 0;
    [ObservableProperty] private bool            _isIndeterminate = false;
    [ObservableProperty] private string          _detail      = string.Empty;

    partial void OnStatusChanged(QueueItemStatus value)
    {
        StatusText = value switch
        {
            QueueItemStatus.Pending     => "Pending",
            QueueItemStatus.Downloading => "Downloading…",
            QueueItemStatus.Done        => "Done",
            QueueItemStatus.Failed      => "Failed",
            QueueItemStatus.Cancelled   => "Cancelled",
            _                           => string.Empty,
        };
    }
}
