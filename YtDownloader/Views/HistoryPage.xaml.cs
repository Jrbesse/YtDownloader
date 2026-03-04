using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; } = new HistoryViewModel();

    public HistoryPage()
    {
        InitializeComponent();
    }

    private void OpenHistoryItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
    }

    private void ClearAll_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.ClearHistory();
    }
}
