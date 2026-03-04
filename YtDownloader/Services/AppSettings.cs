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

    private static string SettingsPath
    {
        get
        {
            var localAppData =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrEmpty(localAppData))
                localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA")
                    ?? AppContext.BaseDirectory;

            return Path.Combine(localAppData, "YtDownloader", "settings.json");
        }
    }

    // ── Settings properties ───────────────────────────────────────────────────

    [ObservableProperty] private bool _showDiagnostics = false;
    [ObservableProperty] private string _theme = "System";   // "Light" | "Dark" | "System"

    // Auto-save whenever any setting changes
    partial void OnShowDiagnosticsChanged(bool value) => Save();
    partial void OnThemeChanged(string value) => Save();

    // ── Serialization model ───────────────────────────────────────────────────

    private record SettingsData(bool ShowDiagnostics, string Theme);

    // ── Load / Save ───────────────────────────────────────────────────────────

    private AppSettings()
    {
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

            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data is null) return;

            // Set backing fields directly to avoid triggering Save() during load
            _showDiagnostics = data.ShowDiagnostics;
            _theme           = data.Theme ?? "System";
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

            var data = new SettingsData(ShowDiagnostics, Theme);
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
