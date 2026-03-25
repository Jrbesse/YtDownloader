using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using YtDownloader.Services;

namespace YtDownloader.Tests;

/// <summary>
/// Tests for FfprobeDownloaderService.EnsureAvailableAsync.
/// All tests use an in-memory HttpMessageHandler and a temp directory —
/// no real network calls or process spawning.
/// </summary>
public class FfprobeDownloaderServiceTests : IDisposable
{
    // ── Setup / Teardown ───────────────────────────────────────────────────────

    private readonly string _tempDir;

    public FfprobeDownloaderServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FfprobeTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    private string FfprobePath => Path.Combine(_tempDir, "ffprobe.exe");

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Creates an in-memory zip with one entry at <paramref name="entryPath"/>.</summary>
    private static byte[] BuildFakeZip(string entryPath, byte[] content)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(entryPath);
            using var es = entry.Open();
            es.Write(content);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a release JSON containing the essentials zip asset and a checksums asset.
    /// <paramref name="checksumsUrl"/> defaults to a placeholder; supply a real value when needed.
    /// </summary>
    private static string ReleasesJson(
        string assetName,
        string downloadUrl,
        string checksumsUrl = "https://example.com/checksums.sha256") =>
        $$"""{"assets":[{"name":"{{assetName}}","browser_download_url":"{{downloadUrl}}"},{"name":"checksums.sha256","browser_download_url":"{{checksumsUrl}}"}]}""";

    /// <summary>
    /// Three-request client: releases JSON → checksums file → zip bytes.
    /// The checksums content is derived automatically from the zip's actual SHA256.
    /// </summary>
    private static HttpClient BuildSuccessClient(string releasesJson, string assetName, byte[] zipBytes)
    {
        var hash             = Convert.ToHexString(SHA256.HashData(zipBytes)).ToLowerInvariant();
        var checksumsContent = $"{hash}  {assetName}\n";

        var handler = new SequentialMockHandler(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(releasesJson, Encoding.UTF8, "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(checksumsContent),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(zipBytes),
            },
        ]);
        return new HttpClient(handler);
    }

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureAvailableAsync_WhenFfprobeAlreadyExists_ReturnsTrueWithoutHttpCall()
    {
        File.WriteAllBytes(FfprobePath, [0x4D, 0x5A]); // stub PE header

        var callCount = 0;
        var http = new HttpClient(new LambdaMockHandler(_ =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }));
        var svc = new FfprobeDownloaderService(http, FfprobePath);

        var result = await svc.EnsureAvailableAsync();

        result.Should().BeTrue();
        callCount.Should().Be(0, "no HTTP call should occur when ffprobe already exists");
    }

    [Fact]
    public async Task EnsureAvailableAsync_WhenFfprobeMissing_DownloadsAndExtractsCorrectly()
    {
        var assetName   = "ffmpeg-n7.1-latest-win64-lgpl-essentials_build.zip";
        var fakeContent = "fake-ffprobe-binary"u8.ToArray();
        var zipBytes    = BuildFakeZip("ffmpeg-build/bin/ffprobe.exe", fakeContent);
        var json        = ReleasesJson(assetName, "https://example.com/build.zip");

        var svc    = new FfprobeDownloaderService(BuildSuccessClient(json, assetName, zipBytes), FfprobePath);
        var result = await svc.EnsureAvailableAsync();

        result.Should().BeTrue();
        File.Exists(FfprobePath).Should().BeTrue();
        File.ReadAllBytes(FfprobePath).Should().Equal(fakeContent);
    }

    [Fact]
    public async Task EnsureAvailableAsync_WhenNoMatchingAssetInRelease_ReturnsFalse()
    {
        var json = ReleasesJson("ffmpeg-win32-other-build.zip", "https://example.com/wrong.zip");
        var http = new HttpClient(new SequentialMockHandler(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            },
        ]));
        var svc      = new FfprobeDownloaderService(http, FfprobePath);
        var messages = new List<string>();
        svc.StatusChanged += messages.Add;

        var result = await svc.EnsureAvailableAsync();

        result.Should().BeFalse();
        File.Exists(FfprobePath).Should().BeFalse();
        messages.Should().Contain(m => m.Contains("Could not locate"));
    }

    [Fact]
    public async Task EnsureAvailableAsync_WhenHttpThrows_ReturnsFalseWithoutThrowing()
    {
        var http = new HttpClient(new LambdaMockHandler(
            _ => throw new HttpRequestException("Network error")));
        var svc      = new FfprobeDownloaderService(http, FfprobePath);
        var messages = new List<string>();
        svc.StatusChanged += messages.Add;

        var result = await svc.EnsureAvailableAsync();

        result.Should().BeFalse();
        messages.Should().Contain(m => m.Contains("failed"));
    }

    [Fact]
    public async Task EnsureAvailableAsync_WhenZipHasNoFfprobeEntry_ReturnsFalseAndCleansUpTempZip()
    {
        var assetName = "ffmpeg-n7.1-latest-win64-lgpl-essentials_build.zip";
        var zipBytes  = BuildFakeZip("ffmpeg-build/bin/ffmpeg.exe", "not-ffprobe"u8.ToArray());
        var json      = ReleasesJson(assetName, "https://example.com/build.zip");

        var svc      = new FfprobeDownloaderService(BuildSuccessClient(json, assetName, zipBytes), FfprobePath);
        var messages = new List<string>();
        svc.StatusChanged += messages.Add;

        var result = await svc.EnsureAvailableAsync();

        result.Should().BeFalse();
        File.Exists(FfprobePath).Should().BeFalse();
        File.Exists(FfprobePath + ".zip.tmp").Should().BeFalse("temp zip must be cleaned up");
        messages.Should().Contain(m => m.Contains("not found inside"));
    }

    [Fact]
    public async Task EnsureAvailableAsync_WhenCancelled_ReturnsFalseWithoutThrowingOperationCancelledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var http = new HttpClient(new LambdaMockHandler(
            _ => Task.FromException<HttpResponseMessage>(new OperationCanceledException())));
        var svc = new FfprobeDownloaderService(http, FfprobePath);

        var act = async () => await svc.EnsureAvailableAsync(cts.Token);

        await act.Should().NotThrowAsync();
        (await svc.EnsureAvailableAsync(cts.Token)).Should().BeFalse();
    }

    [Fact]
    public async Task EnsureAvailableAsync_FiresStatusChangedEventsInOrder()
    {
        var assetName   = "ffmpeg-n7.1-latest-win64-lgpl-essentials_build.zip";
        var fakeContent = "binary"u8.ToArray();
        var zipBytes    = BuildFakeZip("bin/ffprobe.exe", fakeContent);
        var json        = ReleasesJson(assetName, "https://example.com/build.zip");

        var svc      = new FfprobeDownloaderService(BuildSuccessClient(json, assetName, zipBytes), FfprobePath);
        var messages = new List<string>();
        svc.StatusChanged += messages.Add;

        await svc.EnsureAvailableAsync();

        // Verify presence
        var iNotFound    = messages.FindIndex(m => m.Contains("not found"));
        var iDownloading = messages.FindIndex(m => m.Contains("Downloading"));
        var iSuccess     = messages.FindIndex(m => m.Contains("successfully"));

        iNotFound   .Should().BeGreaterThanOrEqualTo(0, "\"not found\" message must be emitted");
        iDownloading.Should().BeGreaterThanOrEqualTo(0, "\"Downloading\" message must be emitted");
        iSuccess    .Should().BeGreaterThanOrEqualTo(0, "\"successfully\" message must be emitted");

        // Verify ordering
        iNotFound.Should().BeLessThan(iDownloading,
            "\"not found\" must fire before \"Downloading\"");
        iDownloading.Should().BeLessThan(iSuccess,
            "\"Downloading\" must fire before \"successfully\"");
    }

    [Fact]
    public async Task EnsureAvailableAsync_AssetAndEntryNameMatchingIsCaseInsensitive()
    {
        var assetName   = "FFMPEG-N7.1-LATEST-WIN64-LGPL-ESSENTIALS_BUILD.ZIP";
        var fakeContent = "binary"u8.ToArray();
        var zipBytes    = BuildFakeZip("bin/FFPROBE.EXE", fakeContent);
        var json        = ReleasesJson(assetName, "https://example.com/build.zip");

        var svc    = new FfprobeDownloaderService(BuildSuccessClient(json, assetName, zipBytes), FfprobePath);
        var result = await svc.EnsureAvailableAsync();

        result.Should().BeTrue("matching should be case-insensitive");
        File.Exists(FfprobePath).Should().BeTrue();
    }

    // ── Mock HTTP helpers ──────────────────────────────────────────────────────

    /// <summary>Returns responses from a pre-built list in order.</summary>
    private sealed class SequentialMockHandler(IReadOnlyList<HttpResponseMessage> responses)
        : HttpMessageHandler
    {
        private int _index;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_index >= responses.Count)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            return Task.FromResult(responses[_index++]);
        }
    }

    /// <summary>Delegates each request to a caller-supplied lambda.</summary>
    private sealed class LambdaMockHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request);
    }
}
