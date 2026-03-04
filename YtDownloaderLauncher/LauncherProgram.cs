using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YtDownloaderLauncher;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    static void Main()
    {
        var launcherDir = AppContext.BaseDirectory;
        var mainExe = Path.Combine(launcherDir, "YtDownloader", "YtDownloader.exe");

        if (!File.Exists(mainExe))
        {
            MessageBox(
                IntPtr.Zero,
                "Could not find YtDownloader.exe.\n\nPlease make sure the \"YtDownloader\" folder is in the same location as this launcher.",
                "YT Downloader - Error",
                0x10 /* MB_ICONERROR */);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName         = mainExe,
            WorkingDirectory = Path.GetDirectoryName(mainExe)!,
            UseShellExecute  = false,
        });
    }
}
