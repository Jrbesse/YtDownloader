# YtDownloader

The main WinUI 3 application project. For full setup, build, and architecture
documentation see the [root README](../README.md).

---

## Assets (Required before building)

Four binaries must be placed in `Assets/` before the project will run:

1. **yt-dlp.exe** → https://github.com/yt-dlp/yt-dlp/releases/latest
2. **ffmpeg.exe** → https://www.gyan.dev/ffmpeg/builds/ (essentials build)
3. **ffprobe.exe** → https://www.gyan.dev/ffmpeg/builds/ (essentials build, same package as ffmpeg)
4. **AtomicParsley.exe** → https://github.com/wez/atomicparsley/releases/latest

---

## Project Structure

```
YtDownloader/
├── Assets/             yt-dlp.exe, ffmpeg.exe, ffprobe.exe, AtomicParsley.exe go here
├── Models/             Plain data classes
├── Services/           yt-dlp subprocess, history, settings, auto-updater
├── ViewModels/         MVVM logic for each page
├── Views/              XAML pages
└── Themes/             App-wide style overrides
```
