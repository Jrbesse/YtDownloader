namespace YtDownloader.Services;

/// <summary>
/// Reads text from the system clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Returns the clipboard's text content, or <c>null</c> if the clipboard
    /// is empty or does not contain text.
    /// </summary>
    Task<string?> GetTextAsync();
}
