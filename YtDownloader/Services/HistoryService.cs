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
    public static readonly HistoryService Instance = new();

    private static readonly string StoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YtDownloader",
        "history.json");

    public ObservableCollection<DownloadHistoryItem> Items { get; } = new();

    private HistoryService()
    {
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
            if (!File.Exists(StoragePath)) return;
            var json = File.ReadAllText(StoragePath);
            var items = JsonSerializer.Deserialize<List<DownloadHistoryItem>>(json);
            if (items is null) return;
            foreach (var item in items)
                Items.Add(item);
        }
        catch { /* Silently ignore corrupt history */ }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StoragePath)!);
            var json = JsonSerializer.Serialize(Items.ToList(),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StoragePath, json);
        }
        catch { /* Silently ignore save errors */ }
    }
}
