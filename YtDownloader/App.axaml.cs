using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using YtDownloader.Services;
using YtDownloader.ViewModels;

namespace YtDownloader;

public partial class App : Application
{
    // ViewModels are created once here and accessed by MainWindow/pages via DataContext.
    public DownloadViewModel  DownloadViewModel  { get; private set; } = null!;
    public AdvancedViewModel  AdvancedViewModel  { get; private set; } = null!;
    public HistoryViewModel   HistoryViewModel   { get; private set; } = null!;
    public SettingsViewModel  SettingsViewModel  { get; private set; } = null!;

    public static new App Current => (App)Application.Current!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var notifications = new NotificationService();
            var fileReveal    = new FileRevealService();
            var folderPicker  = new AvaloniaFolderPickerService();
            var clipboard     = new AvaloniaClipboardService();

            DownloadViewModel = new DownloadViewModel(notifications, fileReveal, folderPicker, clipboard);
            AdvancedViewModel = new AdvancedViewModel(notifications, fileReveal, folderPicker);
            HistoryViewModel  = new HistoryViewModel();
            SettingsViewModel = new SettingsViewModel();

            ApplyTheme(AppSettings.Instance.Theme);
            AppSettings.Instance.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppSettings.Instance.Theme))
                    ApplyTheme(AppSettings.Instance.Theme);
            };

            desktop.MainWindow = new MainWindow();

            _ = Task.Run(() => YtDlpUpdaterService.CheckAndUpdateAsync());
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyTheme(string theme) =>
        RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark"  => ThemeVariant.Dark,
            _       => ThemeVariant.Default,
        };
}
