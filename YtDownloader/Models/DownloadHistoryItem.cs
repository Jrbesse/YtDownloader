namespace YtDownloader.Models;

public class DownloadHistoryItem
{
    public string Title { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }

    // Computed display properties for the ListView binding
    public string SubText => $"{Format} · {Quality}";
    public string DateText => CompletedAt.Date == DateTime.Today
        ? $"Today, {CompletedAt:h:mm tt}"
        : CompletedAt.ToString("MMM d, yyyy");
}
