using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryService _history = HistoryService.Instance;

    public ObservableCollection<DownloadHistoryItem> HistoryItems => _history.Items;

    public Visibility EmptyVisibility =>
        _history.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
}
