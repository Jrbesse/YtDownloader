namespace YtDownloader.Services;

/// <summary>
/// Prompts the user to choose a folder, e.g. for the download output directory.
/// </summary>
public interface IFolderPickerService
{
    /// <summary>
    /// Shows a folder picker and returns the chosen path, or <c>null</c> if
    /// the user cancelled.
    /// </summary>
    Task<string?> PickFolderAsync();
}
