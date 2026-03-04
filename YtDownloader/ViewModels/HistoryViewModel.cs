using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
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

    public Visibility EmptyVisibility =>
        _history.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DiagnosticsVisibility =>
        AppSettings.Instance.ShowDiagnostics ? Visibility.Visible : Visibility.Collapsed;

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

        // Re-evaluate DiagnosticsVisibility when the setting changes
        AppSettings.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppSettings.Instance.ShowDiagnostics))
                OnPropertyChanged(nameof(DiagnosticsVisibility));
        };
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    public void ClearHistory()
    {
        _history.Clear();
    }
}
