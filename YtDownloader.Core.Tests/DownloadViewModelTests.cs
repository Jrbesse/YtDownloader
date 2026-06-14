using FluentAssertions;
using YtDownloader.ViewModels;

namespace YtDownloader.Core.Tests;

/// <summary>
/// Tests for <see cref="DownloadViewModel"/> covering visibility-flag
/// transitions and command behaviors that don't require a live yt-dlp process.
/// </summary>
public class DownloadViewModelTests
{
    private static DownloadViewModel CreateViewModel(
        FakeNotificationService? notifications = null,
        FakeFileRevealService? fileReveal = null,
        FakeFolderPickerService? folderPicker = null,
        FakeClipboardService? clipboard = null)
        => new(
            notifications ?? new FakeNotificationService(),
            fileReveal ?? new FakeFileRevealService(),
            folderPicker ?? new FakeFolderPickerService(),
            clipboard ?? new FakeClipboardService());

    [Fact]
    public void DefaultState_Mp4SelectedAndQualityVisible()
    {
        var vm = CreateViewModel();

        vm.IsMp4Selected.Should().BeTrue();
        vm.SelectedFormat.Should().Be("mp4");
        vm.IsQualityVisible.Should().BeTrue();
        vm.IsDownloadVisible.Should().BeTrue();
        vm.IsProgressVisible.Should().BeFalse();
        vm.IsCancelVisible.Should().BeFalse();
        vm.IsDoneVisible.Should().BeFalse();
        vm.IsVideoInfoVisible.Should().BeFalse();
        vm.IsLogVisible.Should().BeFalse();
        vm.IsUpdateBannerVisible.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, false, "mp3", false)]
    [InlineData(false, true, "wav", false)]
    [InlineData(false, false, "mp4", true)]
    public void IsQualityVisible_TracksAudioFormatSelection(bool mp3, bool wav, string expectedFormat, bool expectedQualityVisible)
    {
        var vm = CreateViewModel();

        vm.IsMp3Selected = mp3;
        vm.IsWavSelected = wav;

        vm.SelectedFormat.Should().Be(expectedFormat);
        vm.IsQualityVisible.Should().Be(expectedQualityVisible);
    }

    [Theory]
    [InlineData("https://example.com/watch?v=abc", false, "")]
    [InlineData("https://example.com/playlist?list=xyz", true, "All videos in this playlist will be downloaded to a subfolder.")]
    public void IsPlaylist_DetectedFromUrl(string url, bool expectedIsPlaylist, string expectedMessage)
    {
        var vm = CreateViewModel();

        vm.Url = url;

        vm.IsPlaylist.Should().Be(expectedIsPlaylist);
        vm.PlaylistMessage.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task PasteFromClipboard_SetsUrlFromClipboard()
    {
        var clipboard = new FakeClipboardService { TextToReturn = "https://example.com/video" };
        var vm = CreateViewModel(clipboard: clipboard);

        await vm.PasteFromClipboardCommand.ExecuteAsync(null);

        vm.Url.Should().Be("https://example.com/video");
    }

    [Fact]
    public async Task PasteFromClipboard_LeavesUrlUnchanged_WhenClipboardEmpty()
    {
        var clipboard = new FakeClipboardService { TextToReturn = null };
        var vm = CreateViewModel(clipboard: clipboard);
        vm.Url = "https://example.com/existing";

        await vm.PasteFromClipboardCommand.ExecuteAsync(null);

        vm.Url.Should().Be("https://example.com/existing");
    }

    [Fact]
    public async Task BrowseFolder_UpdatesOutputFolder_WhenFolderChosen()
    {
        var folderPicker = new FakeFolderPickerService { FolderToReturn = "/tmp/yt-downloader-tests" };
        var vm = CreateViewModel(folderPicker: folderPicker);

        await vm.BrowseFolderCommand.ExecuteAsync(null);

        vm.OutputFolder.Should().Be("/tmp/yt-downloader-tests");
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
    public void Reset_RestoresInitialState()
    {
        var vm = CreateViewModel();

        vm.VideoTitle         = "Some Title";
        vm.ChannelName        = "Some Channel";
        vm.VideoDuration      = "10:00";
        vm.ThumbnailUrl       = "https://example.com/thumb.jpg";
        vm.PreviewLabel       = "First video in playlist";
        vm.IsVideoInfoVisible = true;
        vm.IsProgressVisible  = true;
        vm.IsCancelVisible    = true;
        vm.IsDownloadVisible  = false;
        vm.IsDoneVisible      = true;
        vm.ProgressValue      = 42;

        vm.ResetCommand.Execute(null);

        vm.Url.Should().BeEmpty();
        vm.VideoTitle.Should().BeEmpty();
        vm.ChannelName.Should().BeEmpty();
        vm.VideoDuration.Should().BeEmpty();
        vm.ThumbnailUrl.Should().BeEmpty();
        vm.PreviewLabel.Should().Be("Video preview");
        vm.IsVideoInfoVisible.Should().BeFalse();
        vm.IsProgressVisible.Should().BeFalse();
        vm.IsCancelVisible.Should().BeTrue();
        vm.IsDownloadVisible.Should().BeFalse();
        vm.IsDoneVisible.Should().BeFalse();
        vm.ProgressValue.Should().Be(0);
    }
}
