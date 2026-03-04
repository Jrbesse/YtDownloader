# YtDownloader

The main WinUI 3 application project. For full setup, build, and architecture 
documentation see the [root README](../README.md).

---

## Assets (Required before building)

Two binaries must be placed in `Assets/` before the project will run:

1. **yt-dlp.exe** → https://github.com/yt-dlp/yt-dlp/releases/latest
2. **ffmpeg.exe** → https://www.gyan.dev/ffmpeg/builds/ (essentials build)

---

## Project Structure
```
YtDownloader/
├── Assets/             yt-dlp.exe and ffmpeg.exe go here
├── Models/             Plain data classes
├── Services/           yt-dlp subprocess, history, settings, auto-updater
├── ViewModels/         MVVM logic for each page
├── Views/              XAML pages
└── Themes/             App-wide style overrides
```
