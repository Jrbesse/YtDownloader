using Microsoft.UI.Xaml.Controls;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class AdvancedPage : Page
{
    public AdvancedViewModel ViewModel { get; } = new AdvancedViewModel();

    public AdvancedPage()
    {
        InitializeComponent();
    }
}
