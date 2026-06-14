using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Specialized;
using System.IO;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryService _history = HistoryService.Instance;

    public System.Collections.ObjectModel.ObservableCollection<DownloadHistoryItem> HistoryItems
        => _history.Items;

    public bool IsEmptyVisible => _history.Items.Count == 0;

    public bool IsDiagnosticsVisible => AppSettings.Instance.ShowDiagnostics;

    public string DiagnosticInfo
    {
        get
        {
            var lines = new List<string>
            {
                $"StoragePath : {HistoryService.StoragePath}",
                $"File exists : {File.Exists(HistoryService.StoragePath)}",
                $"Items loaded: {_history.Items.Count}",
            };
            if (_history.LastLoadInfo  is { } li) lines.Add($"Load info   : {li}");
            if (_history.LastLoadError is { } le) lines.Add($"Load ERROR  : {le}");
            if (_history.LastSaveError is { } se) lines.Add($"Save ERROR  : {se}");
            return string.Join(Environment.NewLine, lines);
        }
    }

    public HistoryViewModel()
    {
        _history.Items.CollectionChanged += OnItemsChanged;

        // Re-evaluate IsDiagnosticsVisible when the setting changes
        AppSettings.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppSettings.Instance.ShowDiagnostics))
                OnPropertyChanged(nameof(IsDiagnosticsVisible));
        };
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmptyVisible));
    }

    public void ClearHistory()
    {
        _history.Clear();
    }
}
