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



        // Re-apply immediately if the user changes it in Settings
        AppSettings.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppSettings.Instance.Theme))
                ApplyTheme(AppSettings.Instance.Theme);
        };

        MainWindow.Activate();

        // Apply the persisted theme before the window is shown
        ApplyTheme(AppSettings.Instance.Theme);

        _ = Task.Run(() => YtDlpUpdaterService.CheckAndUpdateAsync());
    }

    internal static void ApplyTheme(string theme)
    {
        if (MainWindow is null) return;

        var elementTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        MainWindow.RootElement.RequestedTheme = elementTheme;
    }
}
