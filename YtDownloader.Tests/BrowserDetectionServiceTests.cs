using FluentAssertions;
using YtDownloader.Services;

namespace YtDownloader.Tests;

/// <summary>
/// Tests for the pure ProgId-to-browser mapping logic.
/// These tests call the internal MapProgIdToBrowser method directly —
/// no registry access is performed.
/// </summary>
public class BrowserDetectionServiceTests
{
    [Fact]
    public void MapProgIdToBrowser_ChromeHTML_ReturnsChrome()
    {
        BrowserDetectionService.MapProgIdToBrowser("ChromeHTML").Should().Be("chrome");
    }

    [Fact]
    public void MapProgIdToBrowser_MSEdgeHTM_ReturnsEdge()
    {
        BrowserDetectionService.MapProgIdToBrowser("MSEdgeHTM").Should().Be("edge");
    }

    [Fact]
    public void MapProgIdToBrowser_FirefoxURL_WithSuffix_ReturnsFirefox()
    {
        BrowserDetectionService.MapProgIdToBrowser("FirefoxURL-308046B0AF4A39CB").Should().Be("firefox");
    }

    [Fact]
    public void MapProgIdToBrowser_BraveHTML_ReturnsBrave()
    {
        BrowserDetectionService.MapProgIdToBrowser("BraveHTML").Should().Be("brave");
    }

    [Fact]
    public void MapProgIdToBrowser_VivaldiHTM_ReturnsVivaldi()
    {
        BrowserDetectionService.MapProgIdToBrowser("VivaldiHTM").Should().Be("vivaldi");
    }

    [Fact]
    public void MapProgIdToBrowser_ChromiumHTM_ReturnsChromium()
    {
        BrowserDetectionService.MapProgIdToBrowser("ChromiumHTM").Should().Be("chromium");
    }

    [Fact]
    public void MapProgIdToBrowser_OperaStable_ReturnsOpera()
    {
        BrowserDetectionService.MapProgIdToBrowser("OperaStable").Should().Be("opera");
    }

    [Fact]
    public void MapProgIdToBrowser_OperaGXStable_ReturnsOpera()
    {
        BrowserDetectionService.MapProgIdToBrowser("OperaGXStable").Should().Be("opera");
    }

    [Fact]
    public void MapProgIdToBrowser_IEProgId_ReturnsNull()
    {
        BrowserDetectionService.MapProgIdToBrowser("IE.AssocFile.HTM").Should().BeNull();
    }

    [Fact]
    public void MapProgIdToBrowser_UnknownProgId_ReturnsNull()
    {
        BrowserDetectionService.MapProgIdToBrowser("SomeFutureBrowserHTM").Should().BeNull();
    }

    [Fact]
    public void MapProgIdToBrowser_EmptyString_ReturnsNull()
    {
        BrowserDetectionService.MapProgIdToBrowser(string.Empty).Should().BeNull();
    }

    [Fact]
    public void MapProgIdToBrowser_EdgeCheckedBeforeChrome_EdgeWins()
    {
        // A ProgId containing both "MSEdge" and "Chrome" (hypothetical future overlap)
        // should return "edge" because edge is checked first in the if-chain.
        BrowserDetectionService.MapProgIdToBrowser("MSEdgeChrome").Should().Be("edge");
    }

    [Fact]
    public void MapProgIdToBrowser_ChromiumCheckedBeforeChrome_ChromiumWins()
    {
        // "Chromium" contains "Chrome" — must return "chromium" not "chrome"
        BrowserDetectionService.MapProgIdToBrowser("ChromiumHTM").Should().Be("chromium");
    }

    [Theory]
    [InlineData("CHROMEHTML")]
    [InlineData("chromehtml")]
    [InlineData("Chromehtml")]
    public void MapProgIdToBrowser_CaseInsensitive_Chrome(string progId)
    {
        BrowserDetectionService.MapProgIdToBrowser(progId).Should().Be("chrome");
    }

    [Theory]
    [InlineData("FIREFOXURL")]
    [InlineData("firefoxurl")]
    public void MapProgIdToBrowser_CaseInsensitive_Firefox(string progId)
    {
        BrowserDetectionService.MapProgIdToBrowser(progId).Should().Be("firefox");
    }
}
