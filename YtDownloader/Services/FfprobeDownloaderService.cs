using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace YtDownloader.Services;

/// <summary>
/// Downloads ffprobe.exe on demand from a pinned BtbN FFmpeg-Builds GitHub release.
/// Called only when SponsorBlock is enabled; yt-dlp requires ffprobe to determine
/// video duration when removing sponsor segments.
///
/// All failures are caught internally — the download proceeds regardless, and yt-dlp
/// surfaces its own error if ffprobe is still unavailable afterward.
/// </summary>
public sealed class FfprobeDownloaderService
{
    // Pinned to a known-good release to avoid unexpected breakage from "latest" churn.
    private const string ReleasesApiUrl =
        "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/tags/autobuild-2026-03-17-13-11";

    // Targets the smallest Windows build (no ffplay, LGPL-only codecs, ~10 MB zip).
    private const string AssetSuffix        = "win64-lgpl-essentials_build.zip";
    private const string FfprobeEntry       = "ffprobe.exe";
    private const string ChecksumsAssetName = "checksums.sha256";

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
    /// Downloads it from the pinned BtbN GitHub release if missing, verifying SHA256.
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

            var info = await FindReleaseInfoAsync(ct);
            if (info is null)
            {
                StatusChanged?.Invoke("Could not locate ffprobe in the latest FFmpeg release — SponsorBlock may fail.");
                return false;
            }

            StatusChanged?.Invoke("Downloading ffprobe…");
            var success = await DownloadAndExtractAsync(info, ct);
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

    private async Task<ReleaseInfo?> FindReleaseInfoAsync(CancellationToken ct)
    {
        var release = await _http.GetFromJsonAsync<GitHubRelease>(ReleasesApiUrl, ct);
        var assets  = release?.Assets;

        var zipAsset       = assets?.FirstOrDefault(a => a.Name.EndsWith(AssetSuffix,        StringComparison.OrdinalIgnoreCase));
        var checksumsAsset = assets?.FirstOrDefault(a => a.Name.Equals(ChecksumsAssetName,   StringComparison.OrdinalIgnoreCase));

        if (zipAsset is null || checksumsAsset is null)
            return null;

        return new ReleaseInfo(zipAsset.BrowserDownloadUrl, checksumsAsset.BrowserDownloadUrl, zipAsset.Name);
    }

    private async Task<bool> DownloadAndExtractAsync(ReleaseInfo info, CancellationToken ct)
    {
        // 1. Fetch the checksums file first so we abort early if it is unavailable.
        var expectedHash = await FetchExpectedHashAsync(info.AssetName, info.ChecksumsUrl, ct);
        if (expectedHash is null)
        {
            StatusChanged?.Invoke("ffprobe download failed: could not retrieve SHA256 checksum.");
            return false;
        }

        // 2. Download the zip to a temp file.
        var tempZip = _ffprobePath + ".zip.tmp";
        try
        {
            using var response = await _http.GetAsync(info.ZipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
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

        // 3. Verify SHA256 before extraction.
        try
        {
            var actualHash = await ComputeSha256Async(tempZip, ct);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                StatusChanged?.Invoke(
                    $"ffprobe download failed: SHA256 mismatch (expected {expectedHash}, got {actualHash}).");
                TryDelete(tempZip);
                return false;
            }
        }
        catch
        {
            TryDelete(tempZip);
            throw;
        }

        // 4. Extract ffprobe.exe from the verified zip.
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

    /// <summary>
    /// Downloads the checksums file and returns the expected SHA256 hex string
    /// for the asset identified by <paramref name="assetName"/>.
    /// Each line of the file has the form: "&lt;hex&gt;  &lt;filename&gt;"
    /// </summary>
    private async Task<string?> FetchExpectedHashAsync(string assetName, string checksumsUrl, CancellationToken ct)
    {
        using var response = await _http.GetAsync(checksumsUrl, ct);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(ct);

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split("  ", 2);
            if (parts.Length == 2 && parts[1].Trim().Equals(assetName, StringComparison.OrdinalIgnoreCase))
                return parts[0].Trim();
        }

        return null;
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }

    // ── Release info ──────────────────────────────────────────────────────────

    private sealed record ReleaseInfo(string ZipUrl, string ChecksumsUrl, string AssetName);

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
