# YtDownloader Cross-Platform Upgrade Plan

Status: **Phase 2 complete — all 8 stages done. App builds and runs on macOS.**

This document is the agreed plan for turning YtDownloader from a Windows-only
WinUI 3 app into a single-codebase Avalonia app that runs natively on
Windows, macOS, and Linux.

---

## Current progress

### Phase 1 — Core library extraction ✅ complete

All work on branch `avalonia-upgrade`. Committed as of the last merge.

| Task | Result |
|---|---|
| `YtDownloader.Core` class library created | ✅ |
| Models and pure services moved into Core | ✅ |
| `IPlatformPaths` + Windows/macOS/Linux implementations | ✅ |
| `AppSettings`, `HistoryService`, `YtDlpService` moved; paths refactored | ✅ |
| Old `YtDownloader.csproj` wired to Core via `ProjectReference` | ✅ |
| `YtDownloader.Core.Tests` created; test files migrated from old project | ✅ |
| CI workflow updated to run Core tests cross-platform | ✅ |
| Full solution builds and 211 tests pass on net8.0 | ✅ |

### Phase 2 — Avalonia shell (in progress)

Executed as 8 staged steps per the approved plan in `.claude/plans/misty-herding-lerdorf.md`.

| Stage | Description | Result |
|---|---|---|
| 1 | Bump `global.json` + Core/Core.Tests TFM to `net10.0`; add `Avalonia 11.3.17` to Core | ✅ 251 tests pass on net10.0 |
| 2 | Add 4 platform-abstraction interfaces to Core (`INotificationService`, `IFileRevealService`, `IFolderPickerService`, `IClipboardService`) | ✅ |
| 3 | Port 4 ViewModels into Core (`DownloadViewModel`, `AdvancedViewModel`, `HistoryViewModel`, `SettingsViewModel`); `Visibility`→`bool` renames; `DispatcherQueue`→`Dispatcher.UIThread`; inject 4 new interfaces via constructor | ✅ |
| 4 | Add Core.Tests coverage: 4 fake services + 4 ViewModel test classes; 251 tests pass | ✅ |
| 5 | Delete old WinUI/Launcher/Tests projects; `YtDownloader/` now holds only `Assets/*.exe`, `app.ico`, `YtDownloader.sln` | ✅ |
| 6 | Scaffold new Avalonia + FluentAvalonia app project in `YtDownloader/` | ✅ App launches on macOS. FluentAvaloniaTheme entry point: `fa:FluentAvaloniaTheme` XAML element (not StyleInclude). |
| 7 | Port AXAML for `MainWindow` and 4 pages | ✅ NavigationView shell + DownloadPage, AdvancedPage, HistoryPage, SettingsPage. Icons deferred (SymbolIconSource class not surfaced in FA 2.5.1 avares). |
| 8 | Update `YtDownloader.sln` and `.github/workflows/ci.yml` | ✅ YtDownloader project added to sln; `build-app` CI job added (all 3 OSes). |

#### Stage 3 naming reference

The `Visibility`→`bool` renames applied across all four ViewModels:

| Old name | New name |
|---|---|
| `VideoInfoVisibility` | `IsVideoInfoVisible` |
| `PreviewLoadingVisibility` | `IsPreviewLoadingVisible` |
| `QualityVisibility` | `IsQualityVisible` |
| `VideoCodecVisibility` | `IsVideoCodecVisible` |
| `SubtitleOptionsVisibility` | `IsSubtitleOptionsVisible` |
| `ProgressVisibility` | `IsProgressVisible` |
| `CancelVisibility` | `IsCancelVisible` |
| `DoneVisibility` | `IsDoneVisible` |
| `DownloadVisibility` | `IsDownloadVisible` |
| `LogVisibility` | `IsLogVisible` |
| `StopVisibility` | `IsStopVisible` |
| `EmptyVisibility` | `IsEmptyVisible` |
| `DiagnosticsVisibility` | `IsDiagnosticsVisible` |
| `UpdateBannerVisibility` | `IsUpdateBannerVisible` |

`BrowserDetectionService`'s call site was also removed from `DownloadViewModel.Download()` (per §4.5 — no replacement on the simple Download page).

#### Package versions chosen

| Package | Version | Notes |
|---|---|---|
| `Avalonia` | 11.3.17 | Added to Core for `Avalonia.Threading.Dispatcher` |
| `FluentAvaloniaUI` | 2.5.1 | Targets Avalonia 11.x; chosen for Stage 6 |
| `Avalonia.Desktop` | 11.3.17 | For Stage 6 |
| `Avalonia.Fonts.Inter` | 11.3.17 | For Stage 6 |
| `Avalonia.Diagnostics` | 11.3.17 | Debug-only; for Stage 6 |
| `Microsoft.WindowsAppSDK` | TBD (was 1.5.x; need to confirm net10 compat) | For Stage 6 — needed for `WindowsNotificationService` |

#### Known issues / next-session punch list

| # | Issue | Fix |
|---|---|---|
| 1 | **Duplicate Settings button** — FluentAvalonia's `NavigationView` shows its own built-in Settings item at the bottom in addition to the one added in `FooterMenuItems`. The built-in one opens a blank page. | Add `IsSettingsVisible="False"` to the `<fa:NavigationView>` element in `MainWindow.axaml` to suppress the built-in item. |
| 2 | **Nav icons missing** — `NavigationViewItem.Icon` doesn't exist in FA 2.5.1; `IconSource` is the correct property but `SymbolIconSource` type wasn't surfaced. | Investigate `<fa:NavigationViewItem.IconSource>` + `<fa:SymbolIconSource Symbol="…" />` (the class may exist even if `strings` didn't show it clearly) or use `<fa:FontIconSource>` as a fallback. |
| 3 | **Duplicate Cancel button in DownloadPage** — Both the progress section and the outer `IsCancelVisible` button show independently, which can produce two visible Cancel buttons at once. | Remove the standalone Cancel button element; rely only on the one inside the progress `StackPanel`. |

#### What remains in `YtDownloader/Assets/` (not yet removed)

The four bundled Windows tool binaries (`yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`, `AtomicParsley.exe`) are still in `YtDownloader/Assets/`. They will be removed from the repo in Phase 3 (§3 — dependency manager + first-run flow), which is outside the current scope of Phase 2.

---

## 0. Decisions already made

These were settled in discussion and drive everything below — listed here so
the "why" behind later sections is clear.

| Area | Decision |
|---|---|
| UI framework | **Avalonia + FluentAvalonia**, one codebase for all three OSes. WinUI 3 is fully retired, not kept for Windows. |
| Bundled binaries | `yt-dlp`, `ffmpeg`, `ffprobe` are **no longer committed to the repo**. The app downloads the correct binaries for the current OS/arch on first run. |
| AtomicParsley | **Dropped**, provided yt-dlp's built-in (mutagen/ffmpeg-based) thumbnail embedding covers all current formats — see §3.4 for the verification plan. |
| Browser-cookie auto-detect | The "automatically use my default browser's cookies" behavior is **removed from the simple Download page on all platforms**. Manual browser selection remains in Advanced mode only (already exists there). |
| Settings / history / cache storage | **Platform-native paths** per OS (not one Windows-shaped path reused everywhere) — see §4.1. |
| Notifications | Windows keeps native toast notifications. macOS gets a lightweight native notification. **Linux has no notifications in v1.** |
| macOS distribution | Unsigned `.app` for v1 ("right-click → Open"). Apple Developer Program / notarization is a possible future step, not part of this plan. |
| License | **MIT**, added as `LICENSE` at repo root. |
| Git history | Left as-is — binaries simply stop being tracked going forward; no history rewrite/force-push. |
| macOS architecture | `osx-x64` and `osx-arm64` shipped as **two separate** build artifacts (no `lipo` universal binary). |
| Bundle identifier | `dev.jrbesse.ytdownloader`, used for the macOS `.app` `CFBundleIdentifier` and later Linux `.desktop` file. |

---

## 1. Project / repo restructuring

| Today | After upgrade |
|---|---|
| `YtDownloader/` — WinUI 3 app (UI + all logic) | `YtDownloader/` — **Avalonia app** (UI shell + platform-specific services) |
| *(none)* | `YtDownloader.Core/` — **new** class library: models, ViewModels, yt-dlp/ffmpeg orchestration, settings, history, dependency manager. Plain `net8.0`, no UI framework references. |
| `YtDownloaderLauncher/` | **Removed.** Its only job was avoiding a console-flash / setting a custom icon on Windows — Avalonia produces a normal native `WinExe`/`.app`/binary with its own icon, so the wrapper is unnecessary. |
| `Directory.Build.targets` (MSIX/Appx stubs) | **Removed.** Those stubs exist only to work around WinUI's MSIX tooling, which goes away with WinUI. |
| `YtDownloader/app.manifest` | **Removed.** Avalonia handles DPI/theme without a Win32 app manifest. |
| `YtDownloader/Assets/*.exe` (yt-dlp, ffmpeg, ffprobe, AtomicParsley — ~193MB) | **Removed from the repo.** Replaced by the runtime dependency manager (§3). |
| `YtDownloader.Tests/` | Re-targeted to plain `net8.0` (no more `-windows` TFM forced by a WinUI project reference). Runs on any OS in CI. |

No `src/`-style reorganization — the existing flat top-level layout
(`YtDownloader/`, `YtDownloader.Core/`, `YtDownloader.Tests/`) is kept to
minimize churn.

---

## 2. Core library extraction (`YtDownloader.Core`)

Everything that is *already* plain C# moves into `YtDownloader.Core` mostly
unchanged:

- `Models/*` (`DownloadOptions`, `DownloadHistoryItem`, `AdvancedDownloadQueueItem`, `VideoInfo`, `DownloadProgress`) — move as-is.
- `Services/YtDlpService.cs` — move; only the four path properties
  (`YtDlpPath`, `FfmpegPath`, `FfprobePath`, `AtomicParsleyPath`) change, per §3.
- `Services/AppSettings.cs`, `Services/HistoryService.cs` — move; storage
  path logic changes per §4.1.
- `Services/YtDlpUpdateState.cs` — move as-is.
- `Services/YtDlpUpdaterService.cs` — move and **generalize** to cover
  ffmpeg/ffprobe via the new dependency manager (§3.2).
- `ViewModels/*` — move, with two mechanical changes:
  1. **`Microsoft.UI.Xaml.Visibility` properties → `bool`.** Every
     `XxxVisibility` property (e.g. `ProgressVisibility`, `LogVisibility`,
     `QualityVisibility`, `StopVisibility`, `EmptyVisibility`,
     `DiagnosticsVisibility`, etc.) becomes a `bool` (e.g. `IsXxxVisible`).
     Avalonia controls bind `IsVisible` directly to a `bool`, so this
     actually *removes* a layer (no converters needed).
  2. **`Microsoft.UI.Dispatching.DispatcherQueue` → `Avalonia.Threading.Dispatcher.UIThread`.**
     `Dispatcher.UIThread` is a static accessor, so ViewModels no longer
     need a dispatcher handed to them through the constructor.

What stays **out** of Core (UI-layer / platform-specific, see §4):
- `Services/NotificationService.cs`
- `Services/BrowserDetectionService.cs` (removed entirely, see §4.4)
- Folder picker & clipboard code currently inline in `DownloadViewModel`/`AdvancedViewModel`
- `Process.Start("explorer.exe", ...)` "open/reveal folder" calls

---

## 3. Dependency management (yt-dlp / ffmpeg / ffprobe)

This is the biggest behavioral change. Today the four tools live in
`Assets/` next to the exe and are copied in by MSBuild. Going forward they
are fetched at runtime into a per-user, always-writable directory.

### 3.1 Where binaries live at runtime

A new `IPlatformPaths.ToolsDir` (see §4.1) gives a per-OS cache location:

| OS | Tools directory |
|---|---|
| Windows | `%LOCALAPPDATA%\YtDownloader\tools\` |
| macOS | `~/Library/Caches/YtDownloader/tools/` |
| Linux | `$XDG_CACHE_HOME/YtDownloader/tools/` (default `~/.cache/YtDownloader/tools/`) |

This is **strictly better** than today even on Windows: it's writable
regardless of where the app itself is installed (e.g. `Program Files`),
whereas today's self-update only works because the app is distributed as a
portable, user-writable folder.

`YtDlpService.YtDlpPath` / `FfmpegPath` / `FfprobePath` become computed from
`IPlatformPaths.ToolsDir` + the platform-correct executable name
(`yt-dlp.exe` / `yt-dlp`, `ffmpeg.exe` / `ffmpeg`, etc.).
`AtomicParsleyPath` is deleted (§3.4).

### 3.2 `DependencyManagerService` (new, in Core)

A new service responsible for:

1. **Checking** whether `yt-dlp`, `ffmpeg`, `ffprobe` exist in `ToolsDir`.
2. **Downloading** any missing tool from a small **tooling manifest** (below).
3. **Extracting** archives (zip/tar.xz) where the upstream distribution isn't
   a bare executable.
4. **Setting the executable bit** on macOS/Linux via `File.SetUnixFileMode`
   (guarded by `OperatingSystem.IsWindows()`).
5. **Verifying** the result by running `--version`.
6. Exposing progress for the first-run UI (§3.3).

The existing `YtDlpUpdaterService` logic (GitHub Releases API lookup,
atomic-swap-with-`.old`-backup) is generalized and reused for yt-dlp's
self-update; ffmpeg/ffprobe get a manual "Check for updates" in Settings
(they version far less frequently, so no need for a startup check).

### 3.3 Tooling manifest

A small JSON document (embedded as a resource, with an optional remote
override URL fetched and cached the same way the app already checks GitHub)
maps **RID → download info** per tool, so a broken/changed upstream URL can
be fixed by editing one file rather than shipping a new app version:

```json
{
  "yt-dlp": {
    "win-x64":   { "source": "github-release", "repo": "yt-dlp/yt-dlp", "asset": "yt-dlp.exe" },
    "osx-x64":   { "source": "github-release", "repo": "yt-dlp/yt-dlp", "asset": "yt-dlp_macos" },
    "osx-arm64": { "source": "github-release", "repo": "yt-dlp/yt-dlp", "asset": "yt-dlp_macos" },
    "linux-x64": { "source": "github-release", "repo": "yt-dlp/yt-dlp", "asset": "yt-dlp_linux" }
  },
  "ffmpeg": {
    "win-x64":   { "source": "url", "url": "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", "archive": "zip", "extract": ["bin/ffmpeg.exe", "bin/ffprobe.exe"] },
    "osx-x64":   { "source": "evermeet", "tool": "ffmpeg" },
    "osx-arm64": { "source": "evermeet", "tool": "ffmpeg" },
    "linux-x64": { "source": "url", "url": "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz", "archive": "tar.xz", "extract": ["ffmpeg", "ffprobe"] }
  }
}
```

> **Note:** the macOS/Linux URLs above are starting points based on
> commonly-used static-build providers (evermeet.cx for macOS,
> johnvansickle.com for Linux). These need to be validated for availability,
> arm64 support, and licensing (GPL vs LGPL build variant) during
> implementation — flagged again in the open questions at the end.

yt-dlp itself already publishes per-OS/arch binaries via GitHub Releases, so
that part of the manifest is solid today.

### 3.4 First-run setup flow

On launch, if any required tool is missing from `ToolsDir`:

1. Show a simple "Setting up YtDownloader" view (replaces the main UI
   momentarily — not a separate installer).
2. Download yt-dlp + ffmpeg + ffprobe with a progress bar (~100–150MB
   depending on OS).
3. Extract / chmod / verify.
4. Proceed to the normal app.

On subsequent launches this is skipped entirely (existing files found).
The existing background yt-dlp update check on startup continues to work,
just pointed at the new `ToolsDir` path.

### 3.5 Dropping AtomicParsley

Today's code passes `--embed-thumbnail` to yt-dlp for all formats, with a
comment that MP3 embedding "requires AtomicParsley.exe". In current yt-dlp,
thumbnail embedding for `mp3`/`m4a`/`opus`/`flac`/`ogg` is handled by yt-dlp's
bundled mutagen support, and `mp4`/`mkv`/`webm` embedding is done via
ffmpeg — both of which we already ship/download. AtomicParsley is not
strictly required by current yt-dlp versions.

**Proposed solution:** remove `AtomicParsleyPath` and the AtomicParsley row
from Settings → Dependencies entirely, and **verify** `--embed-thumbnail`
still works correctly for every format in `DownloadOptions` (mp3, m4a, opus,
flac, ogg, mp4, mkv, webm) as part of implementation testing. If any format
regresses, the fallback is to scope `EmbedThumbnail` out of that one format
rather than reintroducing a fourth binary.

---

## 4. Platform abstraction layer

Small interfaces in `YtDownloader.Core`, with per-OS implementations living
in the Avalonia app project (`YtDownloader/Platform/Windows`,
`/MacOS`, `/Linux`), selected at startup via `OperatingSystem.IsWindows()` /
`IsMacOS()` / `IsLinux()`.

### 4.1 `IPlatformPaths` — settings / history / tools storage

Replaces the hardcoded `Environment.SpecialFolder.LocalApplicationData` use
in `AppSettings` and `HistoryService` with genuinely platform-idiomatic
locations:

| OS | Config (`settings.json`) | Data (`history.json`) | Tools cache |
|---|---|---|---|
| Windows | `%LOCALAPPDATA%\YtDownloader\` | `%LOCALAPPDATA%\YtDownloader\` | `%LOCALAPPDATA%\YtDownloader\tools\` |
| macOS | `~/Library/Application Support/YtDownloader/` | `~/Library/Application Support/YtDownloader/` | `~/Library/Caches/YtDownloader/tools/` |
| Linux | `$XDG_CONFIG_HOME/YtDownloader/` (default `~/.config/...`) | `$XDG_DATA_HOME/YtDownloader/` (default `~/.local/share/...`) | `$XDG_CACHE_HOME/YtDownloader/tools/` (default `~/.cache/...`) |

Windows keeps its current single folder (config and data co-located, as
today); macOS and Linux get the split that's idiomatic on each platform.

### 4.2 `INotificationService` — download-complete notification

| OS | Implementation |
|---|---|
| Windows | Keep existing `Microsoft.Windows.AppNotifications`-based `NotificationService`, unchanged. |
| macOS | New lightweight implementation shelling out to `osascript -e 'display notification ...'` — works unsigned, no extra dependency. |
| Linux | No-op implementation for v1 (the "Show a notification when a download completes" setting has no effect; could be revisited later via `org.freedesktop.Notifications`/D-Bus). |

### 4.3 `IFileRevealService` — "Open folder" / "reveal in file manager"

Replaces every `Process.Start("explorer.exe", ...)` call
(`HistoryPage.xaml.cs`, `DownloadViewModel.OpenFolder`,
`AdvancedViewModel.OpenFolder`):

| OS | Implementation |
|---|---|
| Windows | `explorer.exe /select,"<path>"` (unchanged) |
| macOS | `open -R "<path>"` (reveals and selects the file in Finder) |
| Linux | `xdg-open "<containing-folder>"` (opens the folder; no universal "select this file" equivalent, consistent with the "good enough" bar set for v1) |

### 4.4 Folder picker & clipboard

WinRT `Windows.Storage.Pickers.FolderPicker` (+ `WinRT.Interop` window-handle
dance) and `Windows.ApplicationModel.DataTransfer.Clipboard` are replaced by
Avalonia's built-in, already-cross-platform APIs:

- Folder picker → `TopLevel.GetTopLevel(this).StorageProvider.OpenFolderPickerAsync(...)`
- Clipboard → `TopLevel.GetTopLevel(this).Clipboard.GetTextAsync()`

No custom abstraction needed — these are framework APIs, not OS APIs.

### 4.5 Browser cookie auto-detection — removed

`BrowserDetectionService` (Windows-registry-based `UserChoice` lookup) and
its call site in `DownloadViewModel` (simple Download page) are **deleted
entirely**, along with `BrowserDetectionServiceTests`. The Advanced page's
existing manual "Use browser cookies" dropdown (`SelectedBrowser` /
`Browsers` in `AdvancedViewModel`) is unaffected and remains the only way to
use browser cookies, on all three platforms.

---

## 5. UI migration (WinUI XAML → Avalonia AXAML)

| Page | Notes |
|---|---|
| `MainWindow` | `NavigationView` → FluentAvalonia `NavigationView` (close API match). `AppWindow.Resize`/`SetIcon` → Avalonia `Window.Width`/`Height`/`Icon`. |
| `DownloadPage` | `InfoBar`, `ProgressRing`, `ProgressBar`, `Expander` all have FluentAvalonia/Avalonia equivalents. The manual `string → BitmapImage` thumbnail conversion is replaced by Avalonia's `Bitmap`/`IImage` loaded via an async converter or code-behind — same shape as today, different types. |
| `AdvancedPage` | `ToggleSwitch`, `ComboBox`, `ListView` (→ `ItemsControl`/`ListBox`), `Expander` — all available in FluentAvalonia. `x:Bind` → Avalonia's compiled bindings (`x:DataType` + `{CompiledBinding}`) or standard `{Binding}`. |
| `HistoryPage` | Same `ListView`/`DataTemplate` shape; `OpenHistoryItem_Click` now calls `IFileRevealService` instead of `explorer.exe` directly. |
| `SettingsPage` | Mostly `ToggleSwitch`/`ComboBox`/`TextBlock` — direct port. AtomicParsley row removed (§3.5). |
| `Themes/Generic.xaml` | Small `NavigationView` brush overrides — recreate as an Avalonia `ResourceDictionary` if still needed once FluentAvalonia's default styling is seen running; likely unnecessary. |
| Theme switching (`App.xaml.cs` `ApplyTheme`) | `ElementTheme` (Light/Dark/Default) → Avalonia `ThemeVariant` (Light/Dark/Default) on `Application.Current.RequestedThemeVariant`. Same three-state mapping from `AppSettings.Theme`. |

`CommunityToolkit.WinUI.UI.Controls` (currently referenced in the `.csproj`)
is **unused** (confirmed via search) — simply dropped, not replaced.

---

## 6. Packaging & CI

### 6.1 Build targets

| OS | RID(s) | Output |
|---|---|---|
| Windows | `win-x64` (matches today; `win-x86`/`win-arm64` stay unbuilt as today) | Self-contained portable folder, zipped — same shape as today's `cleanpublish2.ps1` output, minus the launcher exe. |
| macOS | `osx-x64` and `osx-arm64` as **separate builds** for v1 (a universal binary via `lipo` is a possible later improvement, not required for v1) | `YtDownloader.app` bundle, zipped per-arch. |
| Linux | `linux-x64` | Self-contained tarball (`YtDownloader-linux-x64.tar.gz`) containing the binary, a `.desktop` file, and an icon. An AppImage is a nice-to-have follow-up for double-click launching from file managers, not required for v1. |

### 6.2 CI changes (`.github/workflows/ci.yml`, `release.yml`)

**Guiding rule: the entire build/package/release pipeline lives in GitHub
Actions. No script is ever run locally to produce a release artifact —
`dotnet publish` + packaging steps run inline in workflow YAML (or a
`/build` shell script that is itself invoked *only* by CI, never by hand).**

- **`ci.yml` (runs on every PR to `master`, and every push)**:
  - `test` job: drop the `windows-latest` requirement — once
    `YtDownloader.Tests` targets plain `net8.0`, it runs on `ubuntu-latest`
    (cheaper/faster).
  - `build` job: becomes a **matrix over `windows-latest` / `macos-latest` /
    `ubuntu-latest`**, each running `dotnet build`/`dotnet publish` for its
    RID(s) per §6.1 — this is a compile-only sanity check (no artifacts
    uploaded, no release created), but it means **a PR cannot merge unless
    the app builds on all three OSes**, catching cross-platform regressions
    immediately.
- **`release.yml` (triggered on push to `release-v*`, as today)**:
  - Same SemVer-from-branch-name validation.
  - Matrix over `windows-latest` / `macos-latest` / `ubuntu-latest`, each
    producing its packaged artifact (zip/`.app`/tarball per §6.1) and
    uploading it to the GitHub Release via `softprops/action-gh-release@v2`
    — three (or four, counting both macOS arches) artifacts per release
    instead of one, all produced and published automatically.
  - The `YtDownloaderLauncher` publish step is removed entirely.

### 6.3 Things that go away

- MSIX/Appx tooling (`EnableMsixTooling`, `Directory.Build.targets` stubs,
  `DisableMsixProjectCapabilityAddedByProject`)
- `app.manifest`
- `YtDownloaderLauncher` project
- `cleanpublish2.ps1` — **removed outright**. Its steps (build, stage,
  zip) move directly into `release.yml` as inline workflow steps for each
  OS, so there is no script left that anyone needs to run by hand.

---

## 7. Documentation updates

- **Root `README.md`**: update "A portable Windows desktop application...
  Built with WinUI 3" → cross-platform description; replace the bundled-deps
  table with an explanation of on-first-run downloading; update
  requirements section per-OS; check off "Cross-platform support" in the
  roadmap.
- **`YtDownloader/README.md`**: the "manually download these 4 binaries into
  Assets/" instructions are removed — no longer applicable.
- **`CHANGELOG.md`**: new entry for the v2.0 cross-platform release noting
  the breaking changes (binary distribution model, dropped AtomicParsley,
  dropped browser auto-detect).
- **`LICENSE`**: none currently exists — see open question below.

---

## 8. Suggested phasing

1. ✅ **Core extraction** — create `YtDownloader.Core`, move portable code,
   convert `Visibility` → `bool`, generalize path properties to use
   `IPlatformPaths`. Also includes bumping to net10.0, adding the 4 new
   platform-service interfaces, porting ViewModels, adding ViewModel tests,
   and deleting the old WinUI/Launcher/Tests projects.
2. 🔲 **Avalonia shell stands up on Windows** — new Avalonia app project
   (Stages 6–8 of Phase 2): scaffold csproj + platform service impls,
   port all four pages as AXAML, update `.sln` and CI. Get it
   feature-complete and visually comparable to the current WinUI app on
   Windows only. This is the next and largest single chunk of work.
3. 🔲 **Dependency manager + first-run flow** — implement `DependencyManagerService`
   and the tooling manifest, remove `Assets/*.exe` from the repo, validate
   AtomicParsley removal.
4. 🔲 **Platform abstractions for macOS/Linux** — implement
   `INotificationService` (`osascript`) and `IFileRevealService` (`open -R` /
   `xdg-open`) for macOS and Linux; add macOS/Linux `IPlatformPaths` impls
   (already done — `MacPlatformPaths`/`LinuxPlatformPaths` exist in Core).
5. 🔲 **Packaging & CI matrix** — macOS `.app` + Linux tarball builds, CI
   matrix, release workflow updates.
6. 🔲 **Cleanup & docs** — update `README.md`, `CHANGELOG.md`, add `LICENSE`.

Each phase should leave the app in a buildable, runnable state on at least
Windows, so we're never in a fully-broken intermediate state for long.

---

## 9. Open questions

All open questions have been resolved — see the decisions added to §0
(license: MIT, git history: left as-is, macOS arch: two separate builds,
bundle ID: `dev.jrbesse.ytdownloader`). No outstanding decisions remain
before implementation can begin.
