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
- **SponsorBlock** — automatically remove sponsor segments (ffprobe is bundled)
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
| `ffprobe.exe` | Video duration probing for SponsorBlock — bundled with the app |

No separate installation of these tools is required.

---

## Requirements

- Windows 10 version 1803 (build 17763) or later
- **x64** (the distributed release targets `win-x64` only)

### To build from source
- Visual Studio 2022
- Windows App SDK workload
- .NET 8 SDK

> **Note:** The project file lists x86 and ARM64 platform targets, but neither CI nor the release pipeline currently builds or tests those configurations. Building for non-x64 targets from source is unsupported and may require additional adjustments.

---

## Project Structure

```
YtDownloader/          # Main WinUI 3 application
  Assets/              # Bundled binaries (yt-dlp, ffmpeg, ffprobe, AtomicParsley)
  Models/              # Data models (DownloadOptions, queue items, etc.)
  Services/            # YtDlpService, FfprobeDownloaderService, AppSettings, etc.
  ViewModels/          # MVVM view models (CommunityToolkit.Mvvm)
  Views/               # XAML pages (DownloadPage, AdvancedPage, SettingsPage, HistoryPage)
YtDownloader.Tests/    # xUnit test suite (no network or process calls)
YtDownloaderLauncher/  # Lightweight launcher executable
```

---

## Testing

The project ships a unit test suite under `YtDownloader.Tests/` using **xUnit** and **FluentAssertions**. Tests cover all major services and models using in-memory fakes — no network calls, no spawned processes.

```
dotnet test YtDownloader.Tests/YtDownloader.Tests.csproj -p:Platform=x64
```

CI runs the full test suite (with code coverage) and a Release x64 WinUI build on every push and pull request via GitHub Actions. Releases are published automatically when a `v*.*.*` tag is pushed.

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
