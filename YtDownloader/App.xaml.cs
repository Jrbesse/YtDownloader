using Microsoft.UI.Xaml;
using YtDownloader.Services;

namespace YtDownloader;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        // Silently check for yt-dlp updates in the background on every launch.
        // All errors are caught inside — this will never crash the app.
        _ = Task.Run(() => YtDlpUpdaterService.CheckAndUpdateAsync());
    }
}
