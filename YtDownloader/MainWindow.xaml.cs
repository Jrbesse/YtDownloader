using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YtDownloader.Services;
using YtDownloader.ViewModels;
using YtDownloader.Views;

namespace YtDownloader;

public sealed partial class MainWindow : Window
{
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public MainWindow()
    {
        InitializeComponent();

        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 950));
        this.AppWindow.SetIcon("app.ico");

        // Set theme on RootGrid synchronously before first render
        RootGrid.RequestedTheme = AppSettings.Instance.Theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark"  => ElementTheme.Dark,
            _       => ElementTheme.Default,
        };

        // Reflect the persisted Advanced Mode state immediately
        AdvancedNavItem.Visibility = AppSettings.Instance.IsAdvancedMode
            ? Visibility.Visible
            : Visibility.Collapsed;

        // React when the user toggles Advanced Mode in Settings
        SettingsViewModel.AdvancedModeChanged += OnAdvancedModeChanged;
        Closed += (_, _) => SettingsViewModel.AdvancedModeChanged -= OnAdvancedModeChanged;

        ContentFrame.Navigate(typeof(DownloadPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    public FrameworkElement RootElement => RootGrid;

    private void OnAdvancedModeChanged(bool isEnabled)
    {
        _dispatcher.TryEnqueue(() =>
        {
            AdvancedNavItem.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;

            // If the user turned off Advanced Mode while on the Advanced page, navigate away
            if (!isEnabled && ContentFrame.CurrentSourcePageType == typeof(AdvancedPage))
            {
                ContentFrame.Navigate(typeof(DownloadPage));
                NavView.SelectedItem = NavView.MenuItems[0];
            }
        });
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
                case "advanced":
                    ContentFrame.Navigate(typeof(AdvancedPage));
                    break;
            }
        }
    }
}
