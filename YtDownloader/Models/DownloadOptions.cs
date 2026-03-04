namespace YtDownloader.Models;

public class DownloadOptions
{
    public string Url { get; set; } = string.Empty;
    public string Format { get; set; } = "mp4";  // mp4 | mp3 | webm
    public string Quality { get; set; } = "Best available";
    public string OutputFolder { get; set; } = string.Empty;
    public bool IsPlaylist { get; set; }
}
