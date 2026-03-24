using FluentAssertions;
using YtDownloader.Models;

namespace YtDownloader.Tests;

public class DownloadHistoryItemTests
{
    // ── Default values ─────────────────────────────────────────────────────────

    [Fact]
    public void DefaultTitle_IsEmptyString()
    {
        new DownloadHistoryItem().Title.Should().BeEmpty();
    }

    [Fact]
    public void DefaultUrl_IsEmptyString()
    {
        new DownloadHistoryItem().Url.Should().BeEmpty();
    }

    [Fact]
    public void DefaultOutputPath_IsEmptyString()
    {
        new DownloadHistoryItem().OutputPath.Should().BeEmpty();
    }

    [Fact]
    public void DefaultFormat_IsEmptyString()
    {
        new DownloadHistoryItem().Format.Should().BeEmpty();
    }

    [Fact]
    public void DefaultQuality_IsEmptyString()
    {
        new DownloadHistoryItem().Quality.Should().BeEmpty();
    }

    [Fact]
    public void DefaultCompletedAt_IsDateTimeDefault()
    {
        new DownloadHistoryItem().CompletedAt.Should().Be(default(DateTime));
    }

    // ── SubText ────────────────────────────────────────────────────────────────

    [Fact]
    public void SubText_ReturnsFormatDotQuality()
    {
        var item = new DownloadHistoryItem { Format = "MP4", Quality = "1080p" };
        item.SubText.Should().Be("MP4 · 1080p");
    }

    [Fact]
    public void SubText_EmptyFormatAndQuality_ReturnsJustSeparator()
    {
        var item = new DownloadHistoryItem { Format = "", Quality = "" };
        item.SubText.Should().Be(" · ");
    }

    // ── DateText ───────────────────────────────────────────────────────────────

    [Fact]
    public void DateText_Today_ReturnsTodayFormat()
    {
        var item = new DownloadHistoryItem { CompletedAt = DateTime.Now };
        item.DateText.Should().StartWith("Today,");
    }

    [Fact]
    public void DateText_PastDate_ReturnsMonthDayYear()
    {
        var item = new DownloadHistoryItem { CompletedAt = new DateTime(2024, 1, 15) };
        item.DateText.Should().Be("Jan 15, 2024");
    }

    [Fact]
    public void DateText_TodayAtMidnight_StillReturnsToday()
    {
        var item = new DownloadHistoryItem { CompletedAt = DateTime.Today };
        item.DateText.Should().StartWith("Today,");
    }

    [Fact]
    public void DateText_Yesterday_DoesNotReturnToday()
    {
        var item = new DownloadHistoryItem { CompletedAt = DateTime.Today.AddDays(-1) };
        item.DateText.Should().NotStartWith("Today,");
    }
}
