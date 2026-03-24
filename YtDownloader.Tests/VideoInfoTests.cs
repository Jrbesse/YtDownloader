using FluentAssertions;
using YtDownloader.Models;

namespace YtDownloader.Tests;

public class VideoInfoTests
{
    // ── DurationFormatted ──────────────────────────────────────────────────────

    [Fact]
    public void DurationFormatted_Zero_ReturnsEmptyString()
    {
        new VideoInfo { DurationSeconds = 0 }.DurationFormatted.Should().BeEmpty();
    }

    [Fact]
    public void DurationFormatted_Negative_ReturnsEmptyString()
    {
        new VideoInfo { DurationSeconds = -5 }.DurationFormatted.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1,    "0:01")]
    [InlineData(59,   "0:59")]
    [InlineData(60,   "1:00")]
    [InlineData(90,   "1:30")]
    [InlineData(599,  "9:59")]
    [InlineData(3599, "59:59")]
    public void DurationFormatted_BelowOneHour_UsesMSS(double seconds, string expected)
    {
        new VideoInfo { DurationSeconds = seconds }.DurationFormatted.Should().Be(expected);
    }

    [Theory]
    [InlineData(3600, "1:00:00")]
    [InlineData(3661, "1:01:01")]
    [InlineData(7200, "2:00:00")]
    [InlineData(7384, "2:03:04")]
    public void DurationFormatted_OneHourOrMore_UsesHMMSS(double seconds, string expected)
    {
        new VideoInfo { DurationSeconds = seconds }.DurationFormatted.Should().Be(expected);
    }

    [Fact]
    public void DurationFormatted_3599Seconds_DoesNotUseHourFormat()
    {
        // 3599s is just below 1 hour: format is "59:59" (one colon), not "0:59:59" (two colons)
        new VideoInfo { DurationSeconds = 3599 }.DurationFormatted.Count(c => c == ':').Should().Be(1);
    }

    [Fact]
    public void DurationFormatted_3600Seconds_UsesHourFormat()
    {
        // Boundary: 3600 is exactly 1 hour, must use h:mm:ss
        new VideoInfo { DurationSeconds = 3600 }.DurationFormatted.Count(c => c == ':').Should().Be(2);
    }

    // ── Default property values ────────────────────────────────────────────────

    [Fact]
    public void DefaultTitle_IsEmptyString()
    {
        new VideoInfo().Title.Should().BeEmpty();
    }

    [Fact]
    public void DefaultChannel_IsEmptyString()
    {
        new VideoInfo().Channel.Should().BeEmpty();
    }

    [Fact]
    public void DefaultThumbnailUrl_IsEmptyString()
    {
        new VideoInfo().ThumbnailUrl.Should().BeEmpty();
    }

    [Fact]
    public void DefaultDurationSeconds_IsZero()
    {
        new VideoInfo().DurationSeconds.Should().Be(0);
    }
}
