# YtDownloader

The main WinUI 3 application project. For full setup, build, and architecture
documentation see the [root README](../README.md).

---

## Assets (Required before building)

Three binaries must be placed in `Assets/` before the project will run:

1. **yt-dlp.exe** → https://github.com/yt-dlp/yt-dlp/releases/latest
2. **ffmpeg.exe** → https://www.gyan.dev/ffmpeg/builds/ (essentials build)
3. **AtomicParsley.exe** → https://github.com/wez/atomicparsley/releases/latest

A fourth binary, **ffprobe.exe**, is downloaded automatically from
[BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds) on first use when
SponsorBlock is enabled. It does not need to be placed manually.

---

## Project Structure

```
YtDownloader/
├── Assets/             yt-dlp.exe, ffmpeg.exe, AtomicParsley.exe go here
├── Models/             Plain data classes
├── Services/           yt-dlp subprocess, ffprobe downloader, history, settings, auto-updater
├── ViewModels/         MVVM logic for each page
├── Views/              XAML pages
└── Themes/             App-wide style overrides
```
