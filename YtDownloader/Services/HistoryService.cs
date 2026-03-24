using System.Collections.ObjectModel;
using System.Text.Json;
using YtDownloader.Models;

namespace YtDownloader.Services;

/// <summary>
/// Singleton service that stores download history and persists it to a
/// JSON file in the user's AppData folder between sessions.
/// </summary>
public class HistoryService
{
    private static readonly Lazy<HistoryService> _lazyInstance = new(() => new HistoryService());
    public static HistoryService Instance => _lazyInstance.Value;

    // Computed fresh each call — avoids all static initializer ordering issues.
    // Path.Combine with a null first arg was the source of the NullReferenceException.
    public static string StoragePath
    {
        get
        {
            var localAppData =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrEmpty(localAppData))
                localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? AppContext.BaseDirectory;

            return Path.Combine(localAppData, "YtDownloader", "history.json");
        }
    }

    // Diagnostics
    public string? LastLoadError { get; private set; }
    public string? LastSaveError { get; private set; }
    public string? LastLoadInfo  { get; private set; }

    public ObservableCollection<DownloadHistoryItem> Items { get; } = new();

    private readonly string? _overridePath;

    private HistoryService()
    {
        Load();
    }

    /// <summary>Internal constructor for unit tests — uses the supplied path instead of AppData.</summary>
    internal HistoryService(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path must not be null or whitespace.", nameof(storagePath));
        _overridePath = storagePath;
        Load();
    }

    public void Add(DownloadHistoryItem item)
    {
        Items.Insert(0, item);
        Save();
    }

    public void Clear()
    {
        Items.Clear();
        Save();
    }

    private void Load()
    {
        try
        {
            var path = _overridePath ?? StoragePath;   // single call — consistent for the whole load
            LastLoadInfo = $"Resolved path: {path}";

            if (!File.Exists(path))
            {
                LastLoadInfo += " | File not found — starting with empty history.";
                return;
            }

            var json = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(json))
            {
                LastLoadInfo += " | File is empty.";
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<DownloadHistoryItem>>(json, options);

            if (items is null)
            {
                LastLoadInfo += " | Deserialized list was null.";
                return;
            }

            int loaded = 0, skipped = 0;
            foreach (var item in items)
            {
                if (item is null) { skipped++; continue; }

                item.Title      ??= string.Empty;
                item.Url        ??= string.Empty;
                item.OutputPath ??= string.Empty;
                item.Format     ??= string.Empty;
                item.Quality    ??= string.Empty;

                Items.Add(item);
                loaded++;
            }

            LastLoadInfo += $" | Loaded {loaded} item(s), skipped {skipped} null entries.";
        }
        catch (Exception ex)
        {
            LastLoadError = $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    private void Save()
    {
        try
        {
            var path = _overridePath ?? StoragePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var json = JsonSerializer.Serialize(Items.ToList(),
                new JsonSerializerOptions { WriteIndented = true });

            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, overwrite: true);

            LastSaveError = null;
        }
        catch (Exception ex)
        {
            LastSaveError = $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}
