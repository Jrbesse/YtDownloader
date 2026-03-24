using Microsoft.UI.Xaml.Controls;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

    public string VersionValue { get; } = typeof(SettingsPage).Assembly
        .GetName()
        .Version?
        .ToString() ?? string.Empty;

    public SettingsPage()
    {
        InitializeComponent();
        _ = ViewModel.LoadVersionsAsync();
    }
}
