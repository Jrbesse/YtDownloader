# Changelog

All notable changes to YtDownloader will be documented in this file.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Comprehensive unit test suite (`YtDownloader.Tests`) — xUnit + FluentAssertions, no network calls or process spawning
- GitHub Actions CI workflow — tests (with code coverage) and WinUI build run on every push and PR
- GitHub Actions release workflow — tagged `v*.*.*` pushes auto-create a GitHub Release with ZIP artifact
- Branch protection rules documentation (see `BRANCH_PROTECTION.md`)
- PR template with testing checklist
- `ffprobe.exe` bundled in `Assets/` alongside `ffmpeg.exe` — no longer downloaded on demand

### Fixed

- SponsorBlock downloads failing with exit code 1 ("ffprobe not found") — yt-dlp's `ModifyChaptersPP` requires `ffprobe` to determine video duration when removing sponsor segments; `ffprobe.exe` is now bundled in the release artifact

### Changed

- `YtDlpService.BuildArguments` and `ParseProgress` promoted to `internal` for testability
- `BrowserDetectionService.MapProgIdToBrowser` extracted as `internal static` method
- `HistoryService` and `AppSettings` gained internal test constructors with path injection
- README updated: removed inaccurate ARM64/x86 release support claim; added `ffprobe.exe` to dependency table; added Testing section covering the unit test suite and CI pipeline; updated project structure to include `YtDownloader.Tests/`

---

## [1.2.0] - 2026-03-24

### Added

- Verbose logging toggle in Settings — displays raw yt-dlp output during downloads
- Exception details written to verbose log on download failure
- Auto-detect default browser cookies in simple mode (transparent cookie pass-through)

### Fixed

- Silent yt-dlp error reporting — stderr is now captured and surfaced to the user

---

## [1.1.0] - 2026-03-23

### Added

- Advanced mode with batch download queue
- SponsorBlock removal integration (`--sponsorblock-remove all`)
- Browser cookie selection for age-restricted content
- Playlist range controls (start/end index)
- Video codec override (H.264, H.265/HEVC, AV1, VP9)
- Audio bitrate override (128k, 192k, 320k)
- Subtitle download and embed options
- Thumbnail download and embed options
- Custom yt-dlp output template editor
- Persistent download history with storage diagnostics
- AtomicParsley bundled for MP3 thumbnail embedding

### Changed

- Navigation redesigned to NavigationView with Advanced page toggle in Settings

---

## [1.0.0] - Initial release

### Added

- Single video and playlist download (MP4, MP3, AVI, WAV)
- Quality selection (Best available through 360p)
- Real-time progress display with speed and ETA
- Download cancellation
- Output folder picker with remembered last-used folder
- Video preview card (thumbnail, title, channel, duration)
- Theme support (System / Light / Dark)
- Windows toast notifications on completion
- Silent yt-dlp auto-updater
- Lightweight launcher executable
