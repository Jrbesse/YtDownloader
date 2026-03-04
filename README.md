# YT Downloader

A clean, no-install Windows desktop app for downloading YouTube videos and audio.
Built with Claude Sonnet 4.6 Free, using **WinUI 3 (Windows App SDK)** and **.NET 8**, powered by **yt-dlp** and **ffmpeg**.

---

## 📁 Repository Structure

```
/
├── YtDownloader/               # Main WinUI 3 application
│   ├── Assets/
│   │   ├── yt-dlp.exe          ← You download this (see below)
│   │   └── ffmpeg.exe          ← You download this (see below)
│   ├── Models/
│   ├── Services/
│   ├── ViewModels/
│   ├── Views/
│   ├── Themes/
│   └── YtDownloader.csproj
│
├── YtDownloaderLauncher/       # Lightweight launcher exe (no console flash)
│   ├── LauncherProgram.cs
│   └── YtDownloaderLauncher.csproj
│
└── YtDownloader.sln            # Solution file — open this in Visual Studio
```

### Why two projects?

WinUI 3 self-contained apps require a folder full of runtime files alongside the executable. The **Launcher** is a tiny single-file `.exe` that sits at the top level of the distributed ZIP, finds the `YtDownloader\` subfolder, and starts the main app — giving users a clean, simple double-click experience without exposing the runtime internals.

---

## 🛠 Prerequisites

Install these once on your machine:

| Tool | Download |
|------|----------|
| Visual Studio 2022 (Community is free) | https://visualstudio.microsoft.com/ |
| Windows App SDK workload | Included in VS installer — select **"Windows application development"** |
| .NET 8 SDK | https://dotnet.microsoft.com/download/dotnet/8.0 |

---

## 📦 Bundled Binaries (Required)

yt-dlp and ffmpeg must be placed in `YtDownloader/Assets/` before building.
They ship with the app so end users never need to install anything separately.

1. **yt-dlp.exe** → Download from https://github.com/yt-dlp/yt-dlp/releases/latest
   Get `yt-dlp.exe` and place it at `YtDownloader/Assets/yt-dlp.exe`

2. **ffmpeg.exe** → Download from https://www.gyan.dev/ffmpeg/builds/
   Get the "essentials" build, unzip, and copy `ffmpeg.exe` to `YtDownloader/Assets/ffmpeg.exe`

---

## 🚀 Running the Project

```bash
# Open YtDownloader.sln in Visual Studio 2022 and press F5

# Or from the terminal:
cd YtDownloader
dotnet build
dotnet run
```

---

## 📬 Publishing a Release

A PowerShell publish script is included that builds both projects, stages them into the correct folder layout, and zips everything up ready to distribute:

```powershell
cd YtDownloader
.\cleanpublish2.ps1 -Version "1.0.3"
```

Output is written to `dist\YtDownloader-v1.0.3.zip`. The ZIP structure is:

```
YtDownloader-v1.0.3.zip
├── YtDownloader.exe        ← Launcher — users double-click this
├── README.txt
└── YtDownloader\           ← Main app + runtime — do not delete
```

---

## 🏗 Architecture

- **MVVM** via `CommunityToolkit.Mvvm` — ViewModels use `[ObservableProperty]` and `[RelayCommand]` source generators
- **yt-dlp subprocess** — `YtDlpService` launches `yt-dlp.exe`, reads stdout line-by-line, and parses progress with a regex
- **Dispatcher** — progress callbacks marshal back to the UI thread via `DispatcherQueue`
- **Persistent history** — `HistoryService` serializes to JSON at `%LocalAppData%\YtDownloader\history.json`
- **Persistent settings** — `AppSettings` serializes to JSON at `%LocalAppData%\YtDownloader\settings.json`
- **Auto-updater** — `YtDlpUpdaterService` silently checks GitHub on startup and replaces the bundled `yt-dlp.exe` if a newer version is available

---

## 🔮 Suggested Next Steps

- [ ] Download queue for multiple URLs
- [ ] Subtitle download support
- [ ] Download again from history
- [ ] Support for additional sites beyond YouTube
