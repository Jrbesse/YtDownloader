using Microsoft.UI.Xaml.Controls;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class AdvancedPage : Page
{
    public AdvancedViewModel ViewModel { get; } = new AdvancedViewModel();

    /// <summary>
    /// Creates a new AdvancedPage and initializes its XAML-defined UI components.
    /// </summary>
    public AdvancedPage()
    {
        InitializeComponent();
    }
}
