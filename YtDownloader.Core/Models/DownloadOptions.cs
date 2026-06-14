namespace YtDownloader.Models;

public class DownloadOptions
{
    // ── Core (simple mode) ────────────────────────────────────────────────────
    public string Url          { get; set; } = string.Empty;
    public string Format       { get; set; } = "mp4";  // mp4 | mp3 | avi | wav | mkv | webm | flac | ogg | opus | m4a
    public string Quality      { get; set; } = "Best available";
    public string OutputFolder { get; set; } = string.Empty;
    public bool   IsPlaylist   { get; set; }

    // ── Metadata ──────────────────────────────────────────────────────────────
    /// <summary>Embed title, uploader and upload date via --embed-metadata.</summary>
    public bool EmbedMetadata { get; set; } = false;

    // ── Advanced: codec control ───────────────────────────────────────────────
    /// <summary>Video codec override, e.g. "H.264", "H.265/HEVC", "AV1", "VP9". null = let yt-dlp decide.</summary>
    public string? VideoCodec    { get; set; }
    /// <summary>Audio bitrate override, e.g. "128k", "192k", "320k". null = use format default.</summary>
    public string? AudioBitrate  { get; set; }

    // ── Advanced: SponsorBlock ────────────────────────────────────────────────
    public bool RemoveSponsorBlock { get; set; } = false;

    // ── Advanced: browser cookies ─────────────────────────────────────────────
    /// <summary>Browser name to pull cookies from, e.g. "chrome", "firefox", "edge". null = don't use cookies.</summary>
    public string? CookiesFromBrowser { get; set; }

    // ── Advanced: playlist range ──────────────────────────────────────────────
    public int? PlaylistStart { get; set; }
    public int? PlaylistEnd   { get; set; }

    // ── Advanced: thumbnails ──────────────────────────────────────────────────
    public bool EmbedThumbnail { get; set; } = false;
    public bool WriteThumbnail { get; set; } = false;

    // ── Advanced: subtitles ───────────────────────────────────────────────────
    public bool   WriteSubtitles     { get; set; } = false;
    public bool   EmbedSubtitles     { get; set; } = false;
    public bool   WriteAutoSubtitles { get; set; } = false;
    public string SubtitleLanguage   { get; set; } = "en";

    // ── Advanced: output template ─────────────────────────────────────────────
    /// <summary>Custom yt-dlp output template. null = use the default logic based on IsPlaylist.</summary>
    public string? CustomOutputTemplate { get; set; }
}
