using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using YtDownloader.ViewModels;

namespace YtDownloader.Views;

public sealed partial class DownloadPage : Page
{
    public DownloadViewModel ViewModel { get; } = new DownloadViewModel();

    public DownloadPage()
    {
        InitializeComponent();

        // WinUI 3 won't auto-convert a string URL to ImageSource in XAML bindings
        // so we do it here in code-behind whenever ThumbnailUrl changes
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.ThumbnailUrl)
                && !string.IsNullOrEmpty(ViewModel.ThumbnailUrl))
            {
                ThumbnailImage.Source = new BitmapImage(new Uri(ViewModel.ThumbnailUrl));
            }
        };
    }
}