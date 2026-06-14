using YtDownloader.Services;

namespace YtDownloader.Core.Tests;

/// <summary>Test double for <see cref="IFileRevealService"/> that records revealed paths.</summary>
internal sealed class FakeFileRevealService : IFileRevealService
{
    public List<string> RevealedPaths { get; } = new();

    public void RevealInFileManager(string path) => RevealedPaths.Add(path);
}
