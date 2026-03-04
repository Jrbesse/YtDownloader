# YT Downloader

A clean, user-friendly Windows desktop app for downloading YouTube videos and audio.  
Built with **WinUI 3 (Windows App SDK)** and **.NET 8**, powered by **yt-dlp** and **ffmpeg**.

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

yt-dlp and ffmpeg must be placed in the `Assets/` folder before building.  
They ship with the app so users never need to install anything.

1. **yt-dlp.exe** → Download from https://github.com/yt-dlp/yt-dlp/releases/latest  
   Get `yt-dlp.exe` and place it in `YtDownloader/Assets/yt-dlp.exe`

2. **ffmpeg.exe** → Download from https://www.gyan.dev/ffmpeg/builds/  
   Get the "essentials" build, unzip, and copy `ffmpeg.exe` to `YtDownloader/Assets/ffmpeg.exe`

---

## 🚀 Running the Project

```bash
# Clone / open the folder in Visual Studio 2022
# Then press F5 or click the green Run button

# Or from the terminal:
dotnet build
dotnet run
```

---

## 📁 Project Structure

```
YtDownloader/
├── Assets/
│   ├── yt-dlp.exe          ← You download this (see above)
│   └── ffmpeg.exe          ← You download this (see above)
│
├── Models/
│   ├── DownloadOptions.cs      Options passed to yt-dlp
│   ├── DownloadProgress.cs     Progress update data
│   └── DownloadHistoryItem.cs  A completed download record
│
├── Services/
│   ├── YtDlpService.cs     Runs yt-dlp as a subprocess, parses progress
│   └── HistoryService.cs   Stores download history (persisted to AppData)
│
├── ViewModels/
│   ├── DownloadViewModel.cs    All logic for the Download page
│   ├── HistoryViewModel.cs     History page state
│   └── SettingsViewModel.cs    Settings page state
│
├── Views/
│   ├── DownloadPage.xaml       Main download UI
│   ├── HistoryPage.xaml        Past downloads list
│   └── SettingsPage.xaml       Preferences + dependency versions
│
├── Themes/
│   └── Generic.xaml        App-wide style overrides
│
├── App.xaml / App.xaml.cs      Application entry point
├── MainWindow.xaml / .cs       Shell window with NavigationView
└── YtDownloader.csproj         Project file
```

---

## 🏗 Architecture

- **MVVM** via `CommunityToolkit.Mvvm` — ViewModels use `[ObservableProperty]` and `[RelayCommand]` source generators to eliminate boilerplate
- **yt-dlp subprocess** — `YtDlpService` launches yt-dlp.exe, reads stdout line-by-line, and parses progress with a regex
- **Dispatcher** — progress callbacks marshal back to the UI thread via `DispatcherQueue`
- **Persistent history** — `HistoryService` serializes to JSON in `%LocalAppData%\YtDownloader\history.json`

---

## 🔮 Suggested Next Steps (v2)

- [ ] Video thumbnail fetching (yt-dlp `--write-thumbnail` or YouTube oEmbed API)
- [ ] Download queue for multiple URLs
- [ ] Windows toast notifications on completion
- [ ] Auto-update yt-dlp binary in-app
- [ ] Subtitle download support
- [ ] Dark/light theme toggle
