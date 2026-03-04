namespace YtDownloader.Models;

public class DownloadProgress
{
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public double Percent { get; set; }
    public bool IsIndeterminate { get; set; } = true;
}
