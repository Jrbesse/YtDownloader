using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDownloader.Services;

/// <summary>
/// Lightweight singleton that holds app-wide settings so any ViewModel
/// can read or write them without needing to pass references around.
/// </summary>
public partial class AppSettings : ObservableObject
{
    public static readonly AppSettings Instance = new();

    private AppSettings() { }

    [ObservableProperty] private bool _showDiagnostics = false;
}
