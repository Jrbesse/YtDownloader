using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace YtDownloader.Services;

/// <summary>
/// Checks the yt-dlp GitHub releases API for a newer version and
/// silently replaces the bundled yt-dlp.exe if one is found.
/// Safe to call on startup — all errors are caught and ignored.
/// </summary>
public static class YtDlpUpdaterService
{
    private const string ReleasesApiUrl =
        "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";

    private const string AssetName = "yt-dlp.exe";

    // Raised on the calling thread when an update starts / finishes / fails
    public static event Action<string>? StatusChanged;

    public static async Task CheckAndUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            var ytDlpPath = YtDlpService.YtDlpPath;
            if (!File.Exists(ytDlpPath))
            {
                StatusChanged?.Invoke("yt-dlp not found — skipping update check.");
                return;
            }

            // ── 1. Get current bundled version ────────────────────────────────
            var currentVersion = await GetCurrentVersionAsync(ytDlpPath, ct);
            if (currentVersion is null) return;

            // ── 2. Query GitHub API for latest release ────────────────────────
            StatusChanged?.Invoke("Checking for yt-dlp updates…");
            var latest = await FetchLatestReleaseAsync(ct);
            if (latest is null) return;

            // ── 3. Compare versions ───────────────────────────────────────────
            // yt-dlp versions look like "2025.01.15" — lexicographic compare works
            if (string.Compare(latest.TagName, currentVersion, StringComparison.Ordinal) <= 0)
            {
                StatusChanged?.Invoke($"yt-dlp is up to date ({currentVersion}).");
                return;
            }

            // ── 4. Find the yt-dlp.exe download URL ───────────────────────────
            var asset = latest.Assets?.FirstOrDefault(a =>
                a.Name.Equals(AssetName, StringComparison.OrdinalIgnoreCase));

            if (asset is null)
            {
                StatusChanged?.Invoke("Could not find yt-dlp.exe in latest release.");
                return;
            }

            // ── 5. Download to a temp file then atomically replace ────────────
            StatusChanged?.Invoke($"Updating yt-dlp {currentVersion} → {latest.TagName}…");

            var tempPath = ytDlpPath + ".tmp";
            await DownloadFileAsync(asset.BrowserDownloadUrl, tempPath, ct);

            // Download and swap if yt-dlp is out of date.

            StatusChanged?.Invoke($"Updating yt-dlp {currentVersion} → {latest.TagName}…");
            YtDlpUpdateState.SetUpdating(true);

            var tempPathYtDlp = ytDlpPath + ".tmp";
            await DownloadFileAsync(asset.BrowserDownloadUrl, tempPathYtDlp, ct);

            // Mark the brief swap window — Download button will be disabled here
            YtDlpUpdateState.SetSwappingFile(true);
            try
            {
                var oldPath = ytDlpPath + ".old";
                if (File.Exists(oldPath))
                {
                    try { File.Delete(oldPath); }
                    catch { }
                }
                File.Move(ytDlpPath, oldPath);
                File.Move(tempPathYtDlp, ytDlpPath);
                try { File.Delete(oldPath); }
                catch { }
            }
            finally
            {
                YtDlpUpdateState.SetSwappingFile(false);
                YtDlpUpdateState.SetUpdating(false);
            }

            StatusChanged?.Invoke($"yt-dlp updated to {latest.TagName}.");
        }
        catch (OperationCanceledException)
        {
            YtDlpUpdateState.SetUpdating(false);
            YtDlpUpdateState.SetSwappingFile(false);
        }
        catch (Exception ex)
        {
            YtDlpUpdateState.SetUpdating(false);
            YtDlpUpdateState.SetSwappingFile(false);
            StatusChanged?.Invoke($"Update check failed: {ex.Message}");
        }
    }

    private static async Task<string?> GetCurrentVersionAsync(string path, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi)!;
            var output = await p.StandardOutput.ReadLineAsync(ct);
            await p.WaitForExitAsync(ct);
            return output?.Trim();
        }
        catch { return null; }
    }

    private static async Task<GitHubRelease?> FetchLatestReleaseAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient();
            // GitHub API requires a User-Agent header
            http.DefaultRequestHeaders.Add("User-Agent", "YtDownloader-App");
            http.Timeout = TimeSpan.FromSeconds(10);

            var response = await http.GetFromJsonAsync<GitHubRelease>(ReleasesApiUrl, ct);
            return response;
        }
        catch { return null; }
    }

    private static async Task DownloadFileAsync(string url, string destPath, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "YtDownloader-App");
        http.Timeout = TimeSpan.FromMinutes(5);

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var file   = File.Create(destPath);
        await stream.CopyToAsync(file, ct);
    }

    // ── GitHub API response models ────────────────────────────────────────────

    private class GitHubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

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
