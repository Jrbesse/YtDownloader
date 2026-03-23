# ============================================================
#  cleanpublish2.ps1
#  Run from the YtDownloader project folder (where .csproj lives)
#  Usage:  .\cleanpublish2.ps1
#  Usage:  .\cleanpublish2.ps1 -Version "1.2.0"
#
#  Output structure inside the ZIP:
#    YtDownloader.exe          <- clean launcher (no console flash)
#    README.txt
#    YtDownloader\             <- main app + runtime (do not delete)
# ============================================================

param(
    [string]$Version = "1.0.0"
)

Write-Host "DEBUG: MyParam = '$Version'"

$ErrorActionPreference = "Stop"
$ProjectFile         = Join-Path $PSScriptRoot "YtDownloader.csproj"
$LauncherProjectFile = Join-Path $PSScriptRoot "..\YtDownloaderLauncher\YtDownloaderLauncher.csproj"
$PublishDir          = Join-Path $PSScriptRoot "..\..\publish\portable"
$LauncherPublishDir  = Join-Path $PSScriptRoot "..\..\publish\launcher"
$StagingDir          = Join-Path $PSScriptRoot "..\..\publish\staging"
$DistDir             = Join-Path $PSScriptRoot "..\..\dist"
$ZipName             = "YtDownloader-v$Version.zip"
$ZipPath             = Join-Path $DistDir $ZipName

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  YtDownloader Portable Publisher" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Clean previous output
foreach ($dir in @($PublishDir, $LauncherPublishDir, $StagingDir)) {
    if (Test-Path $dir) {
        Write-Host "Cleaning $(Split-Path $dir -Leaf) ..." -ForegroundColor Yellow
        Remove-Item $dir -Recurse -Force
    }
}

# 2. Publish main app
Write-Host "Publishing main app (self-contained, win-x64, Release)..." -ForegroundColor Yellow
dotnet publish $ProjectFile `
    /p:PublishProfile=portable-win-x64 `
    /p:Version=$Version `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Main app publish failed." -ForegroundColor Red
    exit 1
}

# 3. Publish launcher (single-file, no console window)
if (Test-Path $LauncherProjectFile) {
    Write-Host "Publishing launcher..." -ForegroundColor Yellow
    dotnet publish $LauncherProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:Version=$Version `
        -o $LauncherPublishDir `
        --nologo

    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: Launcher publish failed - falling back to .bat launcher." -ForegroundColor Red
        $useBatFallback = $true
    }
} else {
    Write-Host "WARNING: Launcher project not found at $LauncherProjectFile" -ForegroundColor Red
    Write-Host "         Falling back to .bat launcher." -ForegroundColor Red
    $useBatFallback = $true
}

# 4. Verify yt-dlp and ffmpeg
$ytDlp  = Join-Path $PublishDir "Assets\yt-dlp.exe"
$ffmpeg = Join-Path $PublishDir "Assets\ffmpeg.exe"
if (-not (Test-Path $ytDlp))  { Write-Host "WARNING: Assets\yt-dlp.exe not found." -ForegroundColor Red }
if (-not (Test-Path $ffmpeg)) { Write-Host "WARNING: Assets\ffmpeg.exe not found." -ForegroundColor Red }

# 5. Build staging layout
Write-Host "Building staging layout..." -ForegroundColor Yellow

New-Item -ItemType Directory -Path $StagingDir | Out-Null
$AppDir = Join-Path $StagingDir "YtDownloader"
New-Item -ItemType Directory -Path $AppDir | Out-Null

# Main app goes into YtDownloader\ subfolder (runtime must stay flat here)
Copy-Item "$PublishDir\*" -Destination $AppDir -Recurse -Force
Write-Host "  Copied main app to YtDownloader\" -ForegroundColor Gray

# Place launcher or bat at top level
if (-not $useBatFallback) {
    $launcherExe = Join-Path $LauncherPublishDir "YtDownloader.exe"
    if (Test-Path $launcherExe) {
        Copy-Item $launcherExe -Destination $StagingDir -Force
        Write-Host "  Placed launcher YtDownloader.exe at top level" -ForegroundColor Gray
    } else {
        $useBatFallback = $true
        Write-Host "  Launcher exe not found, falling back to .bat" -ForegroundColor Yellow
    }
}

if ($useBatFallback) {
    @"
@echo off
cd /d "%~dp0YtDownloader"
start "" "YtDownloader.exe"
"@ | Set-Content (Join-Path $StagingDir "YtDownloader - Launch.bat")
    Write-Host "  Created YtDownloader - Launch.bat (fallback)" -ForegroundColor Gray
}

# 6. Write README
@"
YT Downloader v$Version
========================

How to use:
  1. Extract this folder anywhere (Desktop, USB drive, etc.)
  2. Double-click YtDownloader.exe to launch
  3. Paste a YouTube URL and click Download

No installation required. Requires Windows 10 or later.

Tip: Right-click YtDownloader.exe and choose
     "Send to > Desktop (create shortcut)" for easy access.

Note: The "YtDownloader" folder contains required files - do not delete it.
"@ | Set-Content (Join-Path $StagingDir "README.txt")

# 7. Show staging layout
Write-Host ""
Write-Host "Staging layout:" -ForegroundColor Cyan
Get-ChildItem $StagingDir | ForEach-Object {
    if ($_.PSIsContainer) {
        $count = (Get-ChildItem $_.FullName -Recurse -File).Count
        Write-Host "  $($_.Name)\ ($count files)" -ForegroundColor Gray
    } else {
        Write-Host "  $($_.Name)" -ForegroundColor Gray
    }
}

# 8. Zip
if (-not (Test-Path $DistDir)) { New-Item -ItemType Directory -Path $DistDir | Out-Null }
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

Write-Host ""
Write-Host "Creating ZIP: $ZipName ..." -ForegroundColor Yellow
Compress-Archive -Path "$StagingDir\*" -DestinationPath $ZipPath

$zipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Done!" -ForegroundColor Green
Write-Host "  Output: $ZipPath" -ForegroundColor Green
Write-Host "  Size:   $zipSize MB" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Share the ZIP - users extract and double-click YtDownloader.exe" -ForegroundColor Cyan
