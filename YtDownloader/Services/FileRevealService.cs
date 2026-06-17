using System.Diagnostics;
using YtDownloader.Services;

namespace YtDownloader;

internal sealed class FileRevealService : IFileRevealService
{
    public void RevealInFileManager(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", $"-R \"{path}\"");
            else
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "xdg-open",
                    ArgumentList    = { Path.GetDirectoryName(path) ?? path },
                    UseShellExecute = false,
                    CreateNoWindow  = true,
                });
        }
        catch { }
    }
}
