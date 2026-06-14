using FluentAssertions;
using YtDownloader.Models;
using YtDownloader.ViewModels;

namespace YtDownloader.Core.Tests;

/// <summary>
/// Tests for <see cref="AdvancedViewModel"/> covering visibility-flag
/// transitions and queue add/remove behaviors. The queue-processing loop has
/// no seam for mocking <c>YtDlpService</c>, so these tests only assert on
/// state flags rather than end-to-end download processing.
/// </summary>
public class AdvancedViewModelTests
{
    private static AdvancedViewModel CreateViewModel(
        FakeNotificationService? notifications = null,
        FakeFileRevealService? fileReveal = null,
        FakeFolderPickerService? folderPicker = null)
        => new(
            notifications ?? new FakeNotificationService(),
            fileReveal ?? new FakeFileRevealService(),
            folderPicker ?? new FakeFolderPickerService());

    [Fact]
    public void DefaultState_VideoFormatShowsCodecAndQuality()
    {
        var vm = CreateViewModel();

        vm.SelectedFormat.Should().Be("mp4");
        vm.IsVideoCodecVisible.Should().BeTrue();
        vm.IsQualityVisible.Should().BeTrue();
        vm.IsSubtitleOptionsVisible.Should().BeFalse();
        vm.IsQueueRunning.Should().BeFalse();
        vm.IsStopVisible.Should().BeFalse();
        vm.IsLogVisible.Should().BeFalse();
    }

    [Theory]
    [InlineData("mp3", false)]
    [InlineData("webm", true)]
    [InlineData("flac", false)]
    [InlineData("mkv", true)]
    public void IsVideoCodecVisible_And_IsQualityVisible_TrackSelectedFormat(string format, bool expectedVisible)
    {
        var vm = CreateViewModel();

        vm.SelectedFormat = format;

        vm.IsVideoCodecVisible.Should().Be(expectedVisible);
        vm.IsQualityVisible.Should().Be(expectedVisible);
    }

    [Fact]
    public void IsSubtitleOptionsVisible_TracksSubtitlesEnabled()
    {
        var vm = CreateViewModel();

        vm.SubtitlesEnabled = true;
        vm.IsSubtitleOptionsVisible.Should().BeTrue();

        vm.SubtitlesEnabled = false;
        vm.IsSubtitleOptionsVisible.Should().BeFalse();
    }

    [Fact]
    public async Task BrowseFolder_UpdatesOutputFolder_WhenFolderChosen()
    {
        var folderPicker = new FakeFolderPickerService { FolderToReturn = "/tmp/yt-downloader-advanced-tests" };
        var vm = CreateViewModel(folderPicker: folderPicker);

        await vm.BrowseFolderCommand.ExecuteAsync(null);

        vm.OutputFolder.Should().Be("/tmp/yt-downloader-advanced-tests");
    }

    [Fact]
    public async Task BrowseFolder_LeavesOutputFolderUnchanged_WhenCancelled()
    {
        var folderPicker = new FakeFolderPickerService { FolderToReturn = null };
        var vm = CreateViewModel(folderPicker: folderPicker);
        var originalFolder = vm.OutputFolder;

        await vm.BrowseFolderCommand.ExecuteAsync(null);

        vm.OutputFolder.Should().Be(originalFolder);
    }

    [Fact]
    public void OpenFolder_RevealsOutputFolder()
    {
        var fileReveal = new FakeFileRevealService();
        var vm = CreateViewModel(fileReveal: fileReveal);

        vm.OpenFolderCommand.Execute(null);

        fileReveal.RevealedPaths.Should().ContainSingle().Which.Should().Be(vm.OutputFolder);
    }

    [Fact]
    public void CanStartQueue_IsTrue_BeforeQueueRuns()
    {
        var vm = CreateViewModel();

        vm.StartQueueCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task StartQueue_DoesNothing_WhenUrlsTextEmpty()
    {
        var vm = CreateViewModel();
        vm.UrlsText = "   ";

        await vm.StartQueueCommand.ExecuteAsync(null);

        vm.Queue.Should().BeEmpty();
        vm.IsQueueRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartQueue_PopulatesQueueWithOneItemPerUrl()
    {
        var vm = CreateViewModel();
        vm.UrlsText = "https://example.com/video1\nhttps://example.com/video2";

        await vm.StartQueueCommand.ExecuteAsync(null);

        vm.Queue.Should().HaveCount(2);
        vm.Queue.Select(q => q.Url).Should().BeEquivalentTo(new[]
        {
            "https://example.com/video1",
            "https://example.com/video2",
        });

        // Without yt-dlp available, each item finishes as Failed rather than
        // staying Pending/Downloading, and the queue stops running.
        vm.Queue.Should().OnlyContain(q => q.Status == QueueItemStatus.Failed);
        vm.IsQueueRunning.Should().BeFalse();
        vm.IsStopVisible.Should().BeFalse();
    }

    [Fact]
    public void StopQueue_DoesNotThrow_WhenQueueNotRunning()
    {
        var vm = CreateViewModel();

        var act = () => vm.StopQueueCommand.Execute(null);

        act.Should().NotThrow();
    }
}
