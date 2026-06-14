namespace YtDownloader.Models;

public class VideoInfo
{
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }

    public string DurationFormatted => DurationSeconds > 0
        ? TimeSpan.FromSeconds(DurationSeconds).ToString(DurationSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss")
        : string.Empty;
}
