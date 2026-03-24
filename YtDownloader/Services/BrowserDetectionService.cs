using Microsoft.Win32;

namespace YtDownloader.Services;

/// <summary>
/// Detects the Windows default browser and maps it to a yt-dlp
/// --cookies-from-browser argument value. Used by simple mode to
/// transparently pass browser cookies without exposing the option to users.
/// </summary>
public static class BrowserDetectionService
{
    /// <summary>
    /// Returns the yt-dlp browser name for the system's default browser,
    /// or null if the browser is unsupported or detection fails.
    /// Detection is best-effort — any failure silently returns null so the
    /// download still proceeds without cookies rather than crashing.
    /// </summary>
    public static string? GetDefaultBrowserForYtDlp()
    {
        try
        {
            // Windows stores the default browser ProgId under this key.
            // Examples: "ChromeHTML", "MSEdgeHTM", "FirefoxURL-308046B0AF4A39CB",
            //           "BraveHTML", "OperaStable", "OperaGXStable", "ChromiumHTM", "VivaldiHTM"
            var progId = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice",
                "ProgId",
                null) as string;

            if (string.IsNullOrEmpty(progId)) return null;

            // Order matters: check Edge before Chrome (MSEdgeHTM doesn't contain "chrome",
            // but double-checking avoids any future ProgId overlap).
            if (progId.Contains("MSEdge",   StringComparison.OrdinalIgnoreCase)) return "edge";
            if (progId.Contains("Brave",    StringComparison.OrdinalIgnoreCase)) return "brave";
            if (progId.Contains("Vivaldi",  StringComparison.OrdinalIgnoreCase)) return "vivaldi";
            if (progId.Contains("Chromium", StringComparison.OrdinalIgnoreCase)) return "chromium";
            if (progId.Contains("Chrome",   StringComparison.OrdinalIgnoreCase)) return "chrome";
            if (progId.Contains("Firefox",  StringComparison.OrdinalIgnoreCase)) return "firefox";
            if (progId.Contains("Opera",    StringComparison.OrdinalIgnoreCase)) return "opera";

            return null; // Unsupported browser (IE, Safari stub, etc.) — no cookies
        }
        catch
        {
            return null; // Never crash the app over a best-effort feature
        }
    }
}
