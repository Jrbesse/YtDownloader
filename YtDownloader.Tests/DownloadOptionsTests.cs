using FluentAssertions;
using YtDownloader.Models;

namespace YtDownloader.Tests;

public class DownloadOptionsTests
{
    [Fact]
    public void DefaultFormat_IsMp4()
    {
        new DownloadOptions().Format.Should().Be("mp4");
    }

    [Fact]
    public void DefaultQuality_IsBestAvailable()
    {
        new DownloadOptions().Quality.Should().Be("Best available");
    }

    [Fact]
    public void DefaultSubtitleLanguage_IsEn()
    {
        new DownloadOptions().SubtitleLanguage.Should().Be("en");
    }

    [Fact]
    public void DefaultUrl_IsEmptyString()
    {
        new DownloadOptions().Url.Should().BeEmpty();
    }

    [Fact]
    public void DefaultOutputFolder_IsEmptyString()
    {
        new DownloadOptions().OutputFolder.Should().BeEmpty();
    }

    [Fact]
    public void DefaultIsPlaylist_IsFalse()
    {
        new DownloadOptions().IsPlaylist.Should().BeFalse();
    }

    [Fact]
    public void DefaultEmbedMetadata_IsFalse()
    {
        new DownloadOptions().EmbedMetadata.Should().BeFalse();
    }

    [Fact]
    public void DefaultRemoveSponsorBlock_IsFalse()
    {
        new DownloadOptions().RemoveSponsorBlock.Should().BeFalse();
    }

    [Fact]
    public void DefaultEmbedThumbnail_IsFalse()
    {
        new DownloadOptions().EmbedThumbnail.Should().BeFalse();
    }

    [Fact]
    public void DefaultWriteThumbnail_IsFalse()
    {
        new DownloadOptions().WriteThumbnail.Should().BeFalse();
    }

    [Fact]
    public void DefaultWriteSubtitles_IsFalse()
    {
        new DownloadOptions().WriteSubtitles.Should().BeFalse();
    }

    [Fact]
    public void DefaultEmbedSubtitles_IsFalse()
    {
        new DownloadOptions().EmbedSubtitles.Should().BeFalse();
    }

    [Fact]
    public void DefaultWriteAutoSubtitles_IsFalse()
    {
        new DownloadOptions().WriteAutoSubtitles.Should().BeFalse();
    }

    [Fact]
    public void DefaultVideoCodec_IsNull()
    {
        new DownloadOptions().VideoCodec.Should().BeNull();
    }

    [Fact]
    public void DefaultAudioBitrate_IsNull()
    {
        new DownloadOptions().AudioBitrate.Should().BeNull();
    }

    [Fact]
    public void DefaultCookiesFromBrowser_IsNull()
    {
        new DownloadOptions().CookiesFromBrowser.Should().BeNull();
    }

    [Fact]
    public void DefaultCustomOutputTemplate_IsNull()
    {
        new DownloadOptions().CustomOutputTemplate.Should().BeNull();
    }

    [Fact]
    public void DefaultPlaylistStart_IsNull()
    {
        new DownloadOptions().PlaylistStart.Should().BeNull();
    }

    [Fact]
    public void DefaultPlaylistEnd_IsNull()
    {
        new DownloadOptions().PlaylistEnd.Should().BeNull();
    }

    [Fact]
    public void AllPropertiesCanBeSet()
    {
        var opts = new DownloadOptions
        {
            Url                  = "https://example.com",
            Format               = "mkv",
            Quality              = "1080p",
            OutputFolder         = @"C:\Videos",
            IsPlaylist           = true,
            EmbedMetadata        = true,
            VideoCodec           = "H.264",
            AudioBitrate         = "192k",
            RemoveSponsorBlock   = true,
            CookiesFromBrowser   = "firefox",
            PlaylistStart        = 2,
            PlaylistEnd          = 5,
            EmbedThumbnail       = true,
            WriteThumbnail       = true,
            WriteSubtitles       = true,
            EmbedSubtitles       = true,
            WriteAutoSubtitles   = true,
            SubtitleLanguage     = "de",
            CustomOutputTemplate = "%(title)s.%(ext)s",
        };

        opts.Url.Should().Be("https://example.com");
        opts.Format.Should().Be("mkv");
        opts.Quality.Should().Be("1080p");
        opts.OutputFolder.Should().Be(@"C:\Videos");
        opts.IsPlaylist.Should().BeTrue();
        opts.EmbedMetadata.Should().BeTrue();
        opts.VideoCodec.Should().Be("H.264");
        opts.AudioBitrate.Should().Be("192k");
        opts.RemoveSponsorBlock.Should().BeTrue();
        opts.CookiesFromBrowser.Should().Be("firefox");
        opts.PlaylistStart.Should().Be(2);
        opts.PlaylistEnd.Should().Be(5);
        opts.EmbedThumbnail.Should().BeTrue();
        opts.WriteThumbnail.Should().BeTrue();
        opts.WriteSubtitles.Should().BeTrue();
        opts.EmbedSubtitles.Should().BeTrue();
        opts.WriteAutoSubtitles.Should().BeTrue();
        opts.SubtitleLanguage.Should().Be("de");
        opts.CustomOutputTemplate.Should().Be("%(title)s.%(ext)s");
    }
}
