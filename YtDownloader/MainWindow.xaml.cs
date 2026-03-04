using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using YtDownloader.Services;
using YtDownloader.Views;

namespace YtDownloader;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 950));
        this.AppWindow.SetIcon("app.ico");

        // Set theme on RootGrid synchronously before first render
        RootGrid.RequestedTheme = AppSettings.Instance.Theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        ContentFrame.Navigate(typeof(DownloadPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }


    public FrameworkElement RootElement => RootGrid;

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
