namespace YtDownloader.Services;

/// <summary>
/// Opens the platform's file manager to reveal a downloaded file or folder.
/// </summary>
public interface IFileRevealService
{
    /// <summary>Opens the platform file manager, revealing the given path.</summary>
    /// <param name="path">The file or folder path to reveal.</param>
    void RevealInFileManager(string path);
}
