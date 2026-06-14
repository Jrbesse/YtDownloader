using YtDownloader.Services;

namespace YtDownloader.Core.Tests;

/// <summary>Test double for <see cref="IFolderPickerService"/> with a freely settable result.</summary>
internal sealed class FakeFolderPickerService : IFolderPickerService
{
    public string? FolderToReturn { get; set; }

    public Task<string?> PickFolderAsync() => Task.FromResult(FolderToReturn);
}
