# YT Downloader

A portable Windows desktop application for downloading videos and audio from YouTube and other sites supported by yt-dlp.

Built with **WinUI 3**, **.NET 8**, and **yt-dlp**. No installation required — all dependencies are bundled.

---

## Features

### Simple Mode
- Paste a URL and download in one click
- Supports MP4, MP3, AVI, WAV output formats
- Automatically uses your default browser's cookies for age-restricted or members-only content
- Remembers your last output folder

### Advanced Mode
- **Download queue** — queue multiple URLs for sequential processing
- **Extended formats** — MKV, WebM, FLAC, OGG, OPUS, M4A
- **Video codec selection** — H.264, H.265/HEVC, AV1, VP9
- **Audio bitrate control** — 128k, 192k, 320k
- **Metadata embedding** — title, uploader, date via `--embed-metadata`
- **Thumbnail embedding and export**
- **SponsorBlock** — automatically remove sponsor segments
- **Subtitle support** — download, embed, or write auto-generated subtitles
- **Playlist range** — specify start and end items for playlist downloads
- **Custom output templates** — full yt-dlp `--output` template support
- **Cookie source selection** — choose which browser's cookies to use per download

### General
- **Verbose logging** — stream raw yt-dlp output during downloads; full error output on failure
- **Windows toast notifications** on download completion
- **Auto-update check** for yt-dlp on launch
- **Persistent settings and download history**
- **Theme support** — Light, Dark, and System

---

## Bundled Dependencies

| Binary | Purpose |
|--------|---------|
| `yt-dlp.exe` | Core download engine |
| `ffmpeg.exe` | Post-processing and format conversion |
| `AtomicParsley.exe` | Metadata and thumbnail embedding for MP4/M4A |

No separate installation of these tools is required.

---

## Requirements

- Windows 10 version 1803 (build 17763) or later
- x86, x64, or ARM64

### To build from source
- Visual Studio 2022
- Windows App SDK workload
- .NET 8 SDK

---

## Project Structure
```
YtDownloader/          # Main WinUI 3 application
  Assets/              # Bundled binaries (yt-dlp, ffmpeg, AtomicParsley)
  Models/              # Data models (DownloadOptions, queue items, etc.)
  Services/            # YtDlpService, AppSettings, NotificationService, etc.
  ViewModels/          # MVVM view models (CommunityToolkit.Mvvm)
  Views/               # XAML pages (DownloadPage, AdvancedPage, SettingsPage, HistoryPage)
YtDownloaderLauncher/  # Lightweight launcher executable
```

---

## Settings

Settings are persisted to `%LocalAppData%\YtDownloader\settings.json`.

| Setting | Default | Description |
|---------|---------|-------------|
| Theme | System | Light / Dark / System |
| Advanced Mode | Off | Enable the Advanced Mode UI |
| Show Notifications | On | Windows toast notifications on completion |
| Auto-Check Updates | On | Check for a newer yt-dlp on launch |
| Remember Output Folder | On | Restore the last-used output folder |
| Verbose Logging | Off | Show raw yt-dlp output in a log panel |

---

## Roadmap

- [ ] Parallel queue processing (currently sequential)
- [ ] History replay — re-queue a past download with its original options
- [ ] Configurable post-download actions (open folder, play file)
- [ ] Cross-platform support (macOS / Linux)
