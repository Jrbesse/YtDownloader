namespace YtDownloader.Models;

/// <summary>
/// Pure data model — no WinUI types here so JSON deserialization never fails.
/// Visibility conversions belong in the View or ViewModel layer.
/// </summary>
public class DownloadHistoryItem
{
    public string Title      { get; set; } = string.Empty;
    public string Url        { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string Format     { get; set; } = string.Empty;
    public string Quality    { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }

    // Computed display helpers (strings only — safe for JSON round-trip)
    public string SubText  => $"{Format} · {Quality}";
    public string DateText => CompletedAt.Date == DateTime.Today
        ? $"Today, {CompletedAt:h:mm tt}"
        : CompletedAt.ToString("MMM d, yyyy");
}
