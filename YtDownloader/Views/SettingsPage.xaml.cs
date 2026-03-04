using Microsoft.UI.Xaml.Controls;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel.LoadVersionsAsync();
    }
}
