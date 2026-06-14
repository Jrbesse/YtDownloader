using YtDownloader.Services;

namespace YtDownloader.Core.Tests;

/// <summary>Test double for <see cref="IPlatformPaths"/> with freely settable values.</summary>
internal sealed class FakePlatformPaths : IPlatformPaths
{
    public string ConfigDir { get; set; } = string.Empty;
    public string DataDir { get; set; } = string.Empty;
    public string ToolsDir { get; set; } = string.Empty;
    public string ExecutableExtension { get; set; } = string.Empty;
}
