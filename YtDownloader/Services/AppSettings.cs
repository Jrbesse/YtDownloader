using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;

namespace YtDownloader.Services;

/// <summary>
/// Persisted app-wide settings. Saved to
/// %LocalAppData%\YtDownloader\settings.json on every change.
///
/// Add new settings as [ObservableProperty] fields and call Save()
/// in the corresponding partial On___Changed method.
/// </summary>
public partial class AppSettings : ObservableObject
{
    public static readonly AppSettings Instance = new();

    private readonly string? _overridePath;

    private string SettingsPath
    {
        get
        {
            if (_overridePath is not null) return _overridePath;

            var localAppData =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrEmpty(localAppData))
                localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA")
                    ?? AppContext.BaseDirectory;

            return Path.Combine(localAppData, "YtDownloader", "settings.json");
        }
    }

    // ── Settings properties ───────────────────────────────────────────────────

    [ObservableProperty] private bool   _showDiagnostics     = false;
    [ObservableProperty] private string _theme               = "System"; // "Light" | "Dark" | "System"
    [ObservableProperty] private bool   _isAdvancedMode      = false;
    [ObservableProperty] private bool   _showNotifications   = true;
    [ObservableProperty] private bool   _autoCheckUpdates    = true;
    [ObservableProperty] private bool   _rememberOutputFolder = true;
    [ObservableProperty] private string _lastOutputFolder    = string.Empty;
    [ObservableProperty] private bool   _verboseLogging      = false;

    // Auto-save whenever any setting changes
    partial void OnShowDiagnosticsChanged(bool value)      => Save();
    partial void OnThemeChanged(string value)              => Save();
    partial void OnIsAdvancedModeChanged(bool value)       => Save();
    partial void OnShowNotificationsChanged(bool value)    => Save();
    partial void OnAutoCheckUpdatesChanged(bool value)     => Save();
    partial void OnRememberOutputFolderChanged(bool value) => Save();
    partial void OnLastOutputFolderChanged(string value)   => Save();
    partial void OnVerboseLoggingChanged(bool value)       => Save();

    // ── Serialization model ───────────────────────────────────────────────────

    // Non-positional class so adding/removing fields never breaks existing JSON files.
    // System.Text.Json matches by property name, so old files missing new keys will
    // simply use the defaults defined below.
    private class SettingsData
    {
        public bool   ShowDiagnostics      { get; init; } = false;
        public string Theme                { get; init; } = "System";
        public bool   IsAdvancedMode       { get; init; } = false;
        public bool   ShowNotifications    { get; init; } = true;
        public bool   AutoCheckUpdates     { get; init; } = true;
        public bool   RememberOutputFolder { get; init; } = true;
        public string LastOutputFolder     { get; init; } = string.Empty;
        public bool   VerboseLogging       { get; init; } = false;
    }

    // ── Load / Save ───────────────────────────────────────────────────────────

    private AppSettings()
    {
        Load();
    }

    /// <summary>Internal constructor for unit tests — uses the supplied path instead of AppData.</summary>
    internal AppSettings(string settingsPath)
    {
        _overridePath = settingsPath;
        Load();
    }

    private void Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return;

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<SettingsData>(json, opts);
            if (data is null) return;

            // Set backing fields directly to avoid triggering Save() during load
            _showDiagnostics      = data.ShowDiagnostics;
            _theme                = data.Theme ?? "System";
            _isAdvancedMode       = data.IsAdvancedMode;
            _showNotifications    = data.ShowNotifications;
            _autoCheckUpdates     = data.AutoCheckUpdates;
            _rememberOutputFolder = data.RememberOutputFolder;
            _lastOutputFolder     = data.LastOutputFolder ?? string.Empty;
            _verboseLogging       = data.VerboseLogging;
        }
        catch
        {
            // Corrupt settings — silently fall back to defaults
        }
    }

    private void Save()
    {
        try
        {
            var path = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var data = new SettingsData
            {
                ShowDiagnostics      = ShowDiagnostics,
                Theme                = Theme,
                IsAdvancedMode       = IsAdvancedMode,
                ShowNotifications    = ShowNotifications,
                AutoCheckUpdates     = AutoCheckUpdates,
                RememberOutputFolder = RememberOutputFolder,
                LastOutputFolder     = LastOutputFolder,
                VerboseLogging       = VerboseLogging,
            };

            var json = JsonSerializer.Serialize(data,
                new JsonSerializerOptions { WriteIndented = true });

            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            // Never crash the app over a settings save failure
        }
    }
}
