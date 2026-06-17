using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using YtDownloader.ViewModels;

namespace YtDownloader;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        var app = App.Current;
        DownloadPageCtrl.DataContext = app.DownloadViewModel;
        AdvancedPageCtrl.DataContext = app.AdvancedViewModel;
        HistoryPageCtrl.DataContext  = app.HistoryViewModel;
        SettingsPageCtrl.DataContext = app.SettingsViewModel;

        UpdateAdvancedNavVisibility(app.SettingsViewModel.IsAdvancedMode);
        SettingsViewModel.AdvancedModeChanged += isAdvanced =>
            Dispatcher.UIThread.Post(() => UpdateAdvancedNavVisibility(isAdvanced));

        _ = app.SettingsViewModel.LoadVersionsAsync();

        NavView.SelectedItem = DownloadNavItem;
    }

    private void NavView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is not NavigationViewItem item) return;
        var tag = item.Tag?.ToString();

        DownloadPageCtrl.IsVisible = tag == "download";
        AdvancedPageCtrl.IsVisible  = tag == "advanced";
        HistoryPageCtrl.IsVisible   = tag == "history";
        SettingsPageCtrl.IsVisible  = tag == "settings";
    }

    private void UpdateAdvancedNavVisibility(bool isAdvanced)
    {
        AdvancedNavItem.IsVisible = isAdvanced;
        if (!isAdvanced && AdvancedPageCtrl.IsVisible)
            NavView.SelectedItem = DownloadNavItem;
    }
}
