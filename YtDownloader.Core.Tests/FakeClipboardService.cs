using YtDownloader.Services;

namespace YtDownloader.Core.Tests;

/// <summary>Test double for <see cref="IClipboardService"/> with a freely settable result.</summary>
internal sealed class FakeClipboardService : IClipboardService
{
    public string? TextToReturn { get; set; }

    public Task<string?> GetTextAsync() => Task.FromResult(TextToReturn);
}
