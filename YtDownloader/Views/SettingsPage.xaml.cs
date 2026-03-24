using Microsoft.UI.Xaml.Controls;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

    public string versionValue = typeof(SettingsPage).Assembly
        .GetName()
        .Version?
        .ToString() ?? string.Empty;

    /// <summary>
    /// Initializes a new instance of the SettingsPage, sets up the UI, and triggers loading of version information into the view model.
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        ViewModel.LoadVersionsAsync();
    }
}
