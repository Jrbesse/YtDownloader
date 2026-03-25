using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;

namespace YtDownloader.Services;

/// <summary>
/// Downloads ffprobe.exe on demand from the BtbN FFmpeg-Builds GitHub release.
/// Called only when SponsorBlock is enabled; yt-dlp requires ffprobe to determine
/// video duration when removing sponsor segments.
///
/// All failures are caught internally — the download proceeds regardless, and yt-dlp
/// surfaces its own error if ffprobe is still unavailable afterward.
/// </summary>
public sealed class FfprobeDownloaderService
{
    private const string ReleasesApiUrl =
        "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest";

    // Targets the smallest Windows build (no ffplay, LGPL-only codecs, ~10 MB zip).
    private const string AssetSuffix  = "win64-lgpl-essentials_build.zip";
    private const string FfprobeEntry = "ffprobe.exe";

    private static readonly Lazy<FfprobeDownloaderService> _lazyInstance =
        new(() => new FfprobeDownloaderService());
    public static FfprobeDownloaderService Instance => _lazyInstance.Value;

    public event Action<string>? StatusChanged;

    private readonly HttpClient _http;
    private readonly string     _ffprobePath;

    private FfprobeDownloaderService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "YtDownloader-App");
        _http.Timeout = TimeSpan.FromMinutes(5);
        _ffprobePath  = YtDlpService.FfprobePath;
    }

    /// <summary>Internal constructor for unit tests — accepts custom HttpClient and path.</summary>
    internal FfprobeDownloaderService(HttpClient http, string ffprobePath)
    {
        if (string.IsNullOrWhiteSpace(ffprobePath))
            throw new ArgumentException("ffprobePath must not be null or whitespace.", nameof(ffprobePath));
        _http        = http;
        _ffprobePath = Path.GetFullPath(ffprobePath);
    }

    /// <summary>
    /// Ensures ffprobe.exe is present at the expected path.
    /// Downloads it from the BtbN GitHub release if missing.
    /// Returns <c>true</c> if ffprobe is ready; <c>false</c> on any failure.
    /// Never throws.
    /// </summary>
    public async Task<bool> EnsureAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(_ffprobePath))
                return true;

            StatusChanged?.Invoke("ffprobe not found — downloading…");

            var downloadUrl = await FindDownloadUrlAsync(ct);
            if (downloadUrl is null)
            {
                StatusChanged?.Invoke("Could not locate ffprobe in the latest FFmpeg release — SponsorBlock may fail.");
                return false;
            }

            StatusChanged?.Invoke("Downloading ffprobe…");
            var success = await DownloadAndExtractAsync(downloadUrl, ct);
            if (success)
                StatusChanged?.Invoke("ffprobe downloaded successfully.");
            return success;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"ffprobe download failed: {ex.Message}");
            return false;
        }
    }

    private async Task<string?> FindDownloadUrlAsync(CancellationToken ct)
    {
        var release = await _http.GetFromJsonAsync<GitHubRelease>(ReleasesApiUrl, ct);
        var asset   = release?.Assets?.FirstOrDefault(a =>
            a.Name.EndsWith(AssetSuffix, StringComparison.OrdinalIgnoreCase));
        return asset?.BrowserDownloadUrl;
    }

    private async Task<bool> DownloadAndExtractAsync(string url, CancellationToken ct)
    {
        var tempZip = _ffprobePath + ".zip.tmp";
        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream     = File.Create(tempZip);
            await responseStream.CopyToAsync(fileStream, ct);
        }
        catch
        {
            TryDelete(tempZip);
            throw;
        }

        try
        {
            using var zip = ZipFile.OpenRead(tempZip);
            var entry = zip.Entries.FirstOrDefault(e =>
                Path.GetFileName(e.FullName).Equals(FfprobeEntry, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                StatusChanged?.Invoke("ffprobe.exe was not found inside the downloaded zip.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_ffprobePath)!);
            var tempOut = _ffprobePath + ".tmp";
            try
            {
                entry.ExtractToFile(tempOut, overwrite: true);
                File.Move(tempOut, _ffprobePath, overwrite: true);
            }
            catch
            {
                TryDelete(tempOut);
                throw;
            }

            return true;
        }
        finally
        {
            TryDelete(tempZip);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }

    // ── GitHub API response models ────────────────────────────────────────────

    private class GitHubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private class GitHubAsset
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
