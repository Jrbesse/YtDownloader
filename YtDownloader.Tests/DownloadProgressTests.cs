using FluentAssertions;
using YtDownloader.Models;

namespace YtDownloader.Tests;

public class DownloadProgressTests
{
    [Fact]
    public void DefaultStatus_IsEmptyString()
    {
        new DownloadProgress().Status.Should().BeEmpty();
    }

    [Fact]
    public void DefaultDetail_IsEmptyString()
    {
        new DownloadProgress().Detail.Should().BeEmpty();
    }

    [Fact]
    public void DefaultPercent_IsZero()
    {
        new DownloadProgress().Percent.Should().Be(0);
    }

    [Fact]
    public void DefaultIsIndeterminate_IsTrue()
    {
        new DownloadProgress().IsIndeterminate.Should().BeTrue();
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var p = new DownloadProgress
        {
            Status          = "Downloading…",
            Detail          = "1.23MiB at 2.00MiB/s",
            Percent         = 42.5,
            IsIndeterminate = false,
        };

        p.Status.Should().Be("Downloading…");
        p.Detail.Should().Be("1.23MiB at 2.00MiB/s");
        p.Percent.Should().BeApproximately(42.5, 0.001);
        p.IsIndeterminate.Should().BeFalse();
    }
}
