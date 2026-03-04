using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using YtDownloader.Views;

namespace YtDownloader;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Set minimum window size and a comfortable default
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 640));
        this.AppWindow.SetIcon("app.ico");

        // Navigate to the Download page on startup
        ContentFrame.Navigate(typeof(DownloadPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag?.ToString())
            {
                case "download":
                    ContentFrame.Navigate(typeof(DownloadPage));
                    break;
                case "history":
                    ContentFrame.Navigate(typeof(HistoryPage));
                    break;
            }
        }
    }
}
