using FluentAssertions;
using YtDownloader.Models;

namespace YtDownloader.Tests;

public class AdvancedDownloadQueueItemTests
{
    // ── Default values ─────────────────────────────────────────────────────────

    [Fact]
    public void DefaultStatus_IsPending()
    {
        new AdvancedDownloadQueueItem().Status.Should().Be(QueueItemStatus.Pending);
    }

    [Fact]
    public void DefaultStatusText_IsPendingString()
    {
        new AdvancedDownloadQueueItem().StatusText.Should().Be("Pending");
    }

    [Fact]
    public void DefaultPercent_IsZero()
    {
        new AdvancedDownloadQueueItem().Percent.Should().Be(0);
    }

    [Fact]
    public void DefaultIsIndeterminate_IsFalse()
    {
        new AdvancedDownloadQueueItem().IsIndeterminate.Should().BeFalse();
    }

    [Fact]
    public void DefaultDetail_IsEmptyString()
    {
        new AdvancedDownloadQueueItem().Detail.Should().BeEmpty();
    }

    [Fact]
    public void DefaultUrl_IsEmptyString()
    {
        new AdvancedDownloadQueueItem().Url.Should().BeEmpty();
    }

    // ── Status → StatusText mapping (via OnStatusChanged source-generated partial) ──

    [Fact]
    public void Status_SetToDownloading_SetsStatusTextToDownloading()
    {
        var item = new AdvancedDownloadQueueItem();
        item.Status = QueueItemStatus.Downloading;
        item.StatusText.Should().Be("Downloading…");
    }

    [Fact]
    public void Status_SetToDone_SetsStatusTextToDone()
    {
        var item = new AdvancedDownloadQueueItem();
        item.Status = QueueItemStatus.Done;
        item.StatusText.Should().Be("Done");
    }

    [Fact]
    public void Status_SetToFailed_SetsStatusTextToFailed()
    {
        var item = new AdvancedDownloadQueueItem();
        item.Status = QueueItemStatus.Failed;
        item.StatusText.Should().Be("Failed");
    }

    [Fact]
    public void Status_SetToCancelled_SetsStatusTextToCancelled()
    {
        var item = new AdvancedDownloadQueueItem();
        item.Status = QueueItemStatus.Cancelled;
        item.StatusText.Should().Be("Cancelled");
    }

    [Fact]
    public void Status_SetBackToPending_SetsStatusTextToPending()
    {
        var item = new AdvancedDownloadQueueItem();
        item.Status = QueueItemStatus.Downloading;
        item.Status = QueueItemStatus.Pending;
        item.StatusText.Should().Be("Pending");
    }

    // ── PropertyChanged ────────────────────────────────────────────────────────

    [Fact]
    public void Status_Change_RaisesPropertyChangedForStatus()
    {
        var item = new AdvancedDownloadQueueItem();
        var changedProps = new List<string?>();
        item.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName);

        item.Status = QueueItemStatus.Downloading;

        changedProps.Should().Contain(nameof(item.Status));
    }

    [Fact]
    public void Status_Change_RaisesPropertyChangedForStatusText()
    {
        var item = new AdvancedDownloadQueueItem();
        var changedProps = new List<string?>();
        item.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName);

        item.Status = QueueItemStatus.Done;

        changedProps.Should().Contain(nameof(item.StatusText));
    }

    // ── Url property ───────────────────────────────────────────────────────────

    [Fact]
    public void Url_CanBeSetAndRead()
    {
        var item = new AdvancedDownloadQueueItem { Url = "https://example.com/watch?v=123" };
        item.Url.Should().Be("https://example.com/watch?v=123");
    }
}
