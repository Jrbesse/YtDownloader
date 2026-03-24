using FluentAssertions;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.Tests;

/// <summary>
/// Tests for YtDlpService.BuildArguments and ParseProgress.
/// These are internal static methods — no subprocess is spawned.
/// </summary>
public class YtDlpServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static DownloadOptions BasicOptions(string format = "mp4", string quality = "Best available") => new()
    {
        Url          = "https://www.youtube.com/watch?v=test123",
        Format       = format,
        Quality      = quality,
        OutputFolder = @"C:\Downloads",
    };

    private static List<string> Args(DownloadOptions options) =>
        YtDlpService.BuildArguments(options);

    // ── Format: mp4 ────────────────────────────────────────────────────────────

    [Fact]
    public void BuildArguments_Mp4_DefaultQuality_ContainsMergeOutputFormatMp4()
    {
        var args = Args(BasicOptions("mp4"));
        args.Should().ContainInOrder("--merge-output-format", "mp4");
    }

    [Fact]
    public void BuildArguments_Mp4_DefaultQuality_ContainsBestAvailableFilter()
    {
        var args = Args(BasicOptions("mp4", "Best available"));
        var fIndex = args.IndexOf("-f");
        fIndex.Should().BeGreaterThan(-1);
        args[fIndex + 1].Should().Be("bestvideo+bestaudio/best");
    }

    [Theory]
    [InlineData("2160p (4K)", "bestvideo[height<=2160]+bestaudio/best[height<=2160]")]
    [InlineData("1440p",      "bestvideo[height<=1440]+bestaudio/best[height<=1440]")]
    [InlineData("1080p",      "bestvideo[height<=1080]+bestaudio/best[height<=1080]")]
    [InlineData("720p",       "bestvideo[height<=720]+bestaudio/best[height<=720]")]
    [InlineData("480p",       "bestvideo[height<=480]+bestaudio/best[height<=480]")]
    [InlineData("360p",       "bestvideo[height<=360]+bestaudio/best[height<=360]")]
    public void BuildArguments_Mp4_QualityFilter_MapsCorrectly(string quality, string expectedFilter)
    {
        var args = Args(BasicOptions("mp4", quality));
        var fIndex = args.IndexOf("-f");
        fIndex.Should().BeGreaterThan(-1);
        args[fIndex + 1].Should().Be(expectedFilter);
    }

    [Fact]
    public void BuildArguments_Mp4_UnknownQuality_UsesBestvideoPlusBestaudio()
    {
        var args = Args(BasicOptions("mp4", "SomeUnknownQuality"));
        var fIndex = args.IndexOf("-f");
        args[fIndex + 1].Should().Be("bestvideo+bestaudio/best");
    }

    // ── Format: audio-only ─────────────────────────────────────────────────────

    [Fact]
    public void BuildArguments_Mp3_Contains_x_AudioFormatMp3_AudioQuality0()
    {
        var args = Args(BasicOptions("mp3"));
        args.Should().Contain("-x");
        args.Should().ContainInOrder("--audio-format", "mp3");
        args.Should().ContainInOrder("--audio-quality", "0");
    }

    [Fact]
    public void BuildArguments_Mp3_DoesNotContainMergeOutputFormat()
    {
        var args = Args(BasicOptions("mp3"));
        args.Should().NotContain("--merge-output-format");
    }

    [Fact]
    public void BuildArguments_Wav_Contains_x_AudioFormatWav()
    {
        var args = Args(BasicOptions("wav"));
        args.Should().Contain("-x");
        args.Should().ContainInOrder("--audio-format", "wav");
    }

    [Fact]
    public void BuildArguments_Flac_Contains_x_AudioFormatFlac()
    {
        var args = Args(BasicOptions("flac"));
        args.Should().Contain("-x");
        args.Should().ContainInOrder("--audio-format", "flac");
    }

    [Fact]
    public void BuildArguments_Ogg_MapsToVorbisCodecName()
    {
        var args = Args(BasicOptions("ogg"));
        args.Should().Contain("-x");
        args.Should().ContainInOrder("--audio-format", "vorbis");
    }

    [Fact]
    public void BuildArguments_Opus_Contains_x_AudioFormatOpus()
    {
        var args = Args(BasicOptions("opus"));
        args.Should().Contain("-x");
        args.Should().ContainInOrder("--audio-format", "opus");
    }

    [Fact]
    public void BuildArguments_M4a_Contains_x_AudioFormatM4a()
    {
        var args = Args(BasicOptions("m4a"));
        args.Should().Contain("-x");
        args.Should().ContainInOrder("--audio-format", "m4a");
    }

    // ── Format: video containers ────────────────────────────────────────────────

    [Fact]
    public void BuildArguments_Avi_ContainsMergeOutputFormatAvi_AndMpeg4InPostprocessorArgs()
    {
        var args = Args(BasicOptions("avi"));
        args.Should().ContainInOrder("--merge-output-format", "avi");
        var ppIndex = args.IndexOf("--postprocessor-args");
        ppIndex.Should().BeGreaterThan(-1);
        args[ppIndex + 1].Should().Contain("mpeg4");
    }

    [Fact]
    public void BuildArguments_Mkv_ContainsMergeOutputFormatMkv()
    {
        var args = Args(BasicOptions("mkv"));
        args.Should().ContainInOrder("--merge-output-format", "mkv");
    }

    [Fact]
    public void BuildArguments_Webm_ContainsMergeOutputFormatWebm()
    {
        var args = Args(BasicOptions("webm"));
        args.Should().ContainInOrder("--merge-output-format", "webm");
    }

    // ── Codec overrides ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("H.264",      "libx264")]
    [InlineData("H.265/HEVC", "libx265")]
    [InlineData("AV1",        "libaom-av1")]
    [InlineData("VP9",        "libvpx-vp9")]
    public void BuildArguments_VideoCodec_AddsCorrectLibraryToPostprocessorArgs(string codec, string expectedLib)
    {
        var options = BasicOptions("mp4");
        options.VideoCodec = codec;
        var args = Args(options);
        var ppIndex = args.IndexOf("--postprocessor-args");
        ppIndex.Should().BeGreaterThan(-1);
        args[ppIndex + 1].Should().Contain(expectedLib);
    }

    [Fact]
    public void BuildArguments_VideoCodecAuto_DoesNotAddCodecToPostprocessorArgs()
    {
        var options = BasicOptions("mp4");
        options.VideoCodec = "(Auto)";
        var args = Args(options);
        // mp4 still has postprocessor-args for aac, but should not contain -c:v
        var ppIndex = args.IndexOf("--postprocessor-args");
        if (ppIndex >= 0)
            args[ppIndex + 1].Should().NotContain("-c:v");
    }

    [Fact]
    public void BuildArguments_VideoCodecNull_DoesNotAddCodecFlag()
    {
        var options = BasicOptions("mp4");
        options.VideoCodec = null;
        var args = Args(options);
        var ppIndex = args.IndexOf("--postprocessor-args");
        if (ppIndex >= 0)
            args[ppIndex + 1].Should().NotContain("-c:v");
    }

    [Fact]
    public void BuildArguments_AudioBitrate_Mp4_AddsToPostprocessorArgs()
    {
        var options = BasicOptions("mp4");
        options.AudioBitrate = "320k";
        var args = Args(options);
        var ppIndex = args.IndexOf("--postprocessor-args");
        ppIndex.Should().BeGreaterThan(-1);
        args[ppIndex + 1].Should().Contain("-b:a 320k");
    }

    [Theory]
    [InlineData("mp3")]
    [InlineData("wav")]
    [InlineData("flac")]
    [InlineData("ogg")]
    [InlineData("opus")]
    [InlineData("m4a")]
    public void BuildArguments_AudioBitrate_AudioOnlyFormat_DoesNotAddBitrateFlag(string audioFormat)
    {
        var options = BasicOptions(audioFormat);
        options.AudioBitrate = "320k";
        var args = Args(options);
        // Audio-only formats should not get -b:a in postprocessor-args
        var ppIndex = args.IndexOf("--postprocessor-args");
        if (ppIndex >= 0)
            args[ppIndex + 1].Should().NotContain("-b:a 320k");
    }

    [Fact]
    public void BuildArguments_CodecAndBitrate_EmittedAsExactlyOnePostprocessorArgsFlag()
    {
        var options = BasicOptions("mp4");
        options.VideoCodec   = "H.264";
        options.AudioBitrate = "320k";
        var args = Args(options);
        // --postprocessor-args must appear exactly once
        args.Count(a => a == "--postprocessor-args").Should().Be(1);
    }

    // ── Metadata & Features ────────────────────────────────────────────────────

    [Fact]
    public void BuildArguments_EmbedMetadata_True_ContainsFlag()
    {
        var options = BasicOptions();
        options.EmbedMetadata = true;
        Args(options).Should().Contain("--embed-metadata");
    }

    [Fact]
    public void BuildArguments_EmbedMetadata_False_DoesNotContainFlag()
    {
        var options = BasicOptions();
        options.EmbedMetadata = false;
        Args(options).Should().NotContain("--embed-metadata");
    }

    [Fact]
    public void BuildArguments_SponsorBlock_True_ContainsSponsorblockRemoveAll()
    {
        var options = BasicOptions();
        options.RemoveSponsorBlock = true;
        var args = Args(options);
        args.Should().ContainInOrder("--sponsorblock-remove", "all");
    }

    [Fact]
    public void BuildArguments_SponsorBlock_False_DoesNotContainFlag()
    {
        var options = BasicOptions();
        options.RemoveSponsorBlock = false;
        Args(options).Should().NotContain("--sponsorblock-remove");
    }

    [Fact]
    public void BuildArguments_CookiesFromBrowser_Chrome_PassesCookiesFlag()
    {
        var options = BasicOptions();
        options.CookiesFromBrowser = "chrome";
        var args = Args(options);
        args.Should().ContainInOrder("--cookies-from-browser", "chrome");
    }

    [Fact]
    public void BuildArguments_CookiesFromBrowser_None_SkipsCookiesFlag()
    {
        var options = BasicOptions();
        options.CookiesFromBrowser = "(None)";
        Args(options).Should().NotContain("--cookies-from-browser");
    }

    [Fact]
    public void BuildArguments_CookiesFromBrowser_Null_SkipsCookiesFlag()
    {
        var options = BasicOptions();
        options.CookiesFromBrowser = null;
        Args(options).Should().NotContain("--cookies-from-browser");
    }

    [Fact]
    public void BuildArguments_PlaylistStart_AddsPlaylistStartArg()
    {
        var options = BasicOptions();
        options.PlaylistStart = 3;
        var args = Args(options);
        args.Should().ContainInOrder("--playlist-start", "3");
    }

    [Fact]
    public void BuildArguments_PlaylistEnd_AddsPlaylistEndArg()
    {
        var options = BasicOptions();
        options.PlaylistEnd = 10;
        var args = Args(options);
        args.Should().ContainInOrder("--playlist-end", "10");
    }

    [Fact]
    public void BuildArguments_PlaylistStartAndEnd_BothArgsPresent()
    {
        var options = BasicOptions();
        options.PlaylistStart = 2;
        options.PlaylistEnd   = 8;
        var args = Args(options);
        args.Should().ContainInOrder("--playlist-start", "2");
        args.Should().ContainInOrder("--playlist-end",   "8");
    }

    [Fact]
    public void BuildArguments_NoPlaylistRange_NoRangeArgs()
    {
        var args = Args(BasicOptions());
        args.Should().NotContain("--playlist-start");
        args.Should().NotContain("--playlist-end");
    }

    // ── Thumbnails ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildArguments_EmbedThumbnail_True_ContainsFlag()
    {
        var options = BasicOptions();
        options.EmbedThumbnail = true;
        Args(options).Should().Contain("--embed-thumbnail");
    }

    [Fact]
    public void BuildArguments_WriteThumbnail_True_ContainsFlag()
    {
        var options = BasicOptions();
        options.WriteThumbnail = true;
        Args(options).Should().Contain("--write-thumbnail");
    }

    [Fact]
    public void BuildArguments_BothThumbnailFlags_False_NeitherPresent()
    {
        var args = Args(BasicOptions());
        args.Should().NotContain("--embed-thumbnail");
        args.Should().NotContain("--write-thumbnail");
    }

    // ── Subtitles ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildArguments_WriteSubtitles_True_ContainsWriteSubsAndSubLangs()
    {
        var options = BasicOptions();
        options.WriteSubtitles = true;
        var args = Args(options);
        args.Should().Contain("--write-subs");
        args.Should().Contain("--sub-langs");
    }

    [Fact]
    public void BuildArguments_EmbedSubtitles_True_ContainsEmbedSubsNotWriteSubs()
    {
        var options = BasicOptions();
        options.EmbedSubtitles = true;
        var args = Args(options);
        args.Should().Contain("--embed-subs");
        args.Should().NotContain("--write-subs");
    }

    [Fact]
    public void BuildArguments_EmbedSubtitles_TakesPrecedenceOver_WriteSubtitles()
    {
        var options = BasicOptions();
        options.WriteSubtitles = true;
        options.EmbedSubtitles = true;
        var args = Args(options);
        args.Should().Contain("--embed-subs");
        args.Should().NotContain("--write-subs");
    }

    [Fact]
    public void BuildArguments_SubtitleLanguage_CustomValue_PassedToSubLangs()
    {
        var options = BasicOptions();
        options.WriteSubtitles    = true;
        options.SubtitleLanguage  = "fr";
        var args = Args(options);
        args.Should().ContainInOrder("--sub-langs", "fr");
    }

    [Fact]
    public void BuildArguments_SubtitleLanguage_Empty_DefaultsToEn()
    {
        var options = BasicOptions();
        options.WriteSubtitles   = true;
        options.SubtitleLanguage = "";
        var args = Args(options);
        args.Should().ContainInOrder("--sub-langs", "en");
    }

    [Fact]
    public void BuildArguments_WriteAutoSubtitles_True_ContainsFlag()
    {
        var options = BasicOptions();
        options.WriteAutoSubtitles = true;
        Args(options).Should().Contain("--write-auto-subs");
    }

    [Fact]
    public void BuildArguments_NoSubtitleOptions_NoSubtitleFlagsPresent()
    {
        var args = Args(BasicOptions());
        args.Should().NotContain("--write-subs");
        args.Should().NotContain("--embed-subs");
        args.Should().NotContain("--sub-langs");
        args.Should().NotContain("--write-auto-subs");
    }

    // ── Output template ────────────────────────────────────────────────────────

    [Fact]
    public void BuildArguments_IsPlaylist_False_OutputTemplateContainsTitleExt()
    {
        var options = BasicOptions();
        options.IsPlaylist = false;
        var args = Args(options);
        var oIndex = args.IndexOf("-o");
        oIndex.Should().BeGreaterThan(-1);
        args[oIndex + 1].Should().Contain("%(title)s.%(ext)s");
        args[oIndex + 1].Should().NotContain("%(playlist)s");
    }

    [Fact]
    public void BuildArguments_IsPlaylist_True_OutputTemplateContainsPlaylistSubfolder()
    {
        var options = BasicOptions();
        options.IsPlaylist = true;
        var args = Args(options);
        var oIndex = args.IndexOf("-o");
        oIndex.Should().BeGreaterThan(-1);
        args[oIndex + 1].Should().Contain("%(playlist)s");
        args[oIndex + 1].Should().Contain("%(playlist_index)s");
    }

    [Fact]
    public void BuildArguments_CustomOutputTemplate_Absolute_UsedAsIs()
    {
        var options = BasicOptions();
        options.CustomOutputTemplate = @"C:\Videos\%(title)s.%(ext)s";
        var args = Args(options);
        var oIndex = args.IndexOf("-o");
        args[oIndex + 1].Should().Be(@"C:\Videos\%(title)s.%(ext)s");
    }

    [Fact]
    public void BuildArguments_CustomOutputTemplate_Relative_PrependedWithOutputFolder()
    {
        var options = BasicOptions();
        options.OutputFolder         = @"C:\Downloads";
        options.CustomOutputTemplate = @"%(title)s.%(ext)s";
        var args = Args(options);
        var oIndex = args.IndexOf("-o");
        args[oIndex + 1].Should().StartWith(@"C:\Downloads");
        args[oIndex + 1].Should().Contain("%(title)s.%(ext)s");
    }

    [Fact]
    public void BuildArguments_AlwaysContainsNewlineFlag()
    {
        Args(BasicOptions()).Should().Contain("--newline");
    }

    [Fact]
    public void BuildArguments_UrlIsLastArgument()
    {
        var options = BasicOptions();
        var args = Args(options);
        args.Last().Should().Be(options.Url);
    }

    [Fact]
    public void BuildArguments_AlwaysContainsFfmpegLocationFlag()
    {
        var args = Args(BasicOptions());
        var ffmpegIndex = args.IndexOf("--ffmpeg-location");
        ffmpegIndex.Should().BeGreaterThan(-1);
        args[ffmpegIndex + 1].Should().EndWith("ffmpeg.exe");
    }

    // ── ParseProgress ──────────────────────────────────────────────────────────

    [Fact]
    public void ParseProgress_DownloadLine_ReturnsDownloadingStatus()
    {
        var line = "[download]  50.0% of  123.45MiB at  1.23MiB/s ETA 00:30";
        var progress = YtDlpService.ParseProgress(line);
        progress.Status.Should().Be("Downloading…");
    }

    [Fact]
    public void ParseProgress_DownloadLine_Percent50_IsCorrect()
    {
        var line = "[download]  50.0% of  123.45MiB at  1.23MiB/s ETA 00:30";
        var progress = YtDlpService.ParseProgress(line);
        progress.Percent.Should().BeApproximately(50.0, 0.001);
    }

    [Fact]
    public void ParseProgress_DownloadLine_IsNotIndeterminate()
    {
        var line = "[download]  75.3% of   50.00MiB at  2.00MiB/s ETA 00:10";
        var progress = YtDlpService.ParseProgress(line);
        progress.IsIndeterminate.Should().BeFalse();
    }

    [Fact]
    public void ParseProgress_DownloadLine_DetailContainsSizeSpeedEta()
    {
        var line = "[download]  99.9% of  999.99MiB at  9.99MiB/s ETA 00:01";
        var progress = YtDlpService.ParseProgress(line);
        progress.Detail.Should().Contain("999.99MiB");
        progress.Detail.Should().Contain("9.99MiB/s");
        progress.Detail.Should().Contain("ETA 00:01");
    }

    [Fact]
    public void ParseProgress_FfmpegLine_ReturnsMergingStreamsStatus()
    {
        var progress = YtDlpService.ParseProgress("[ffmpeg] Merging formats into output.mp4");
        progress.Status.Should().Be("Merging streams…");
        progress.IsIndeterminate.Should().BeTrue();
    }

    [Fact]
    public void ParseProgress_MergerLine_ReturnsMergingStreamsStatus()
    {
        var progress = YtDlpService.ParseProgress("[Merger] Merging formats into output.mp4");
        progress.Status.Should().Be("Merging streams…");
        progress.IsIndeterminate.Should().BeTrue();
    }

    [Fact]
    public void ParseProgress_DestinationLine_ReturnsStartingDownloadStatus()
    {
        var progress = YtDlpService.ParseProgress("[download] Destination: output.mp4");
        progress.Status.Should().Be("Starting download…");
    }

    [Fact]
    public void ParseProgress_DestinationLine_DetailStripsPrefix()
    {
        var progress = YtDlpService.ParseProgress("[download] Destination: my_video.mp4");
        progress.Detail.Should().NotContain("[download] Destination:");
        progress.Detail.Should().Contain("my_video.mp4");
    }

    [Fact]
    public void ParseProgress_InfoLine_ReturnsFetchingVideoInfoStatus()
    {
        var progress = YtDlpService.ParseProgress("[info] Extracting URL: ...");
        progress.Status.Should().Be("Fetching video info…");
    }

    [Fact]
    public void ParseProgress_YoutubeLine_ReturnsFetchingVideoInfoStatus()
    {
        var progress = YtDlpService.ParseProgress("[youtube] dQw4w9WgXcQ: Downloading webpage");
        progress.Status.Should().Be("Fetching video info…");
    }

    [Fact]
    public void ParseProgress_UnrecognizedLine_ReturnsWorkingStatus()
    {
        var progress = YtDlpService.ParseProgress("Some random yt-dlp output line");
        progress.Status.Should().Be("Working…");
        progress.IsIndeterminate.Should().BeTrue();
    }

    [Fact]
    public void ParseProgress_EmptyString_DoesNotThrow()
    {
        var act = () => YtDlpService.ParseProgress(string.Empty);
        act.Should().NotThrow();
    }

    [Fact]
    public void ParseProgress_EmptyString_ReturnsWorkingStatus()
    {
        var progress = YtDlpService.ParseProgress(string.Empty);
        progress.Status.Should().Be("Working…");
    }
}
