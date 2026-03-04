using Microsoft.UI.Xaml.Controls;
using System.Security.Cryptography.X509Certificates;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

    public string versionValue = typeof(Program).Assembly
    .GetName()
    .Version
    .ToString();

    public SettingsPage()
    {
    InitializeComponent();
    ViewModel.LoadVersionsAsync();
    }
}
