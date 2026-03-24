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

    /// <summary>
    /// Initializes the main application window: configures size and icon, applies persisted UI settings, wires the Advanced Mode change handler, and navigates to the default page.
    /// </summary>
    /// <remarks>
    /// - Sets the window size to 800×950 and the application icon.
    /// - Applies the persisted theme to the root grid before first render.
    /// - Sets the visibility of the Advanced navigation item from persisted settings and subscribes to runtime changes.
    /// - Navigates the content frame to the default Download page and selects the first navigation item.
    /// </remarks>
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

        ContentFrame.Navigate(typeof(DownloadPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    public FrameworkElement RootElement => RootGrid;

    /// <summary>
    /// Updates the UI to reflect whether Advanced Mode is enabled and, if it was disabled while the Advanced page was active, navigates to the Download page.
    /// </summary>
    /// <param name="isEnabled">`true` to enable Advanced Mode (make the Advanced navigation item visible); `false` to disable it (hide the Advanced navigation item).</param>
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

    /// <summary>
    /// Handles selection changes in the navigation view and navigates the ContentFrame to the selected page.
    /// </summary>
    /// <param name="sender">The NavigationView that raised the selection change.</param>
    /// <param name="args">Selection change event data; if settings is selected the handler navigates to the Settings page.</param>
    /// <remarks>
    /// Navigation mapping:
    /// - Tag "download" → DownloadPage
    /// - Tag "history" → HistoryPage
    /// - Tag "advanced" → AdvancedPage
    /// Selecting Settings navigates to SettingsPage immediately.
    /// </remarks>
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
