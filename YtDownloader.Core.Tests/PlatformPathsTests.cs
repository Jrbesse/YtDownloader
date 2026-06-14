using FluentAssertions;
using YtDownloader.Services;

namespace YtDownloader.Core.Tests;

/// <summary>
/// Tests for PlatformPaths.Current default selection and the per-OS
/// IPlatformPaths implementations' path-construction logic.
/// </summary>
public class PlatformPathsTests : IDisposable
{
    public PlatformPathsTests() => PlatformPaths.ResetToDefault();

    public void Dispose() => PlatformPaths.ResetToDefault();

    // ── PlatformPaths.Current ────────────────────────────────────────────────────

    [Fact]
    public void Current_DefaultsToWindowsPlatformPaths_OnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;
        PlatformPaths.Current.Should().BeOfType<WindowsPlatformPaths>();
    }

    [Fact]
    public void Current_DefaultsToMacPlatformPaths_OnMacOS()
    {
        if (!OperatingSystem.IsMacOS()) return;
        PlatformPaths.Current.Should().BeOfType<MacPlatformPaths>();
    }

    [Fact]
    public void Current_DefaultsToLinuxPlatformPaths_OnLinux()
    {
        if (!OperatingSystem.IsLinux()) return;
        PlatformPaths.Current.Should().BeOfType<LinuxPlatformPaths>();
    }

    [Fact]
    public void Current_CanBeOverridden_AndResetToDefault()
    {
        var fake = new FakePlatformPaths();

        PlatformPaths.Current = fake;
        PlatformPaths.Current.Should().BeSameAs(fake);

        PlatformPaths.ResetToDefault();
        PlatformPaths.Current.Should().NotBeSameAs(fake);
    }

    // ── WindowsPlatformPaths ──────────────────────────────────────────────────────

    [Fact]
    public void Windows_ConfigAndDataDir_ShareTheSameYtDownloaderFolder()
    {
        var paths = new WindowsPlatformPaths();
        paths.ConfigDir.Should().EndWith("YtDownloader");
        paths.DataDir.Should().Be(paths.ConfigDir);
    }

    [Fact]
    public void Windows_ToolsDir_IsAssetsFolderNextToApp()
    {
        var paths = new WindowsPlatformPaths();
        paths.ToolsDir.Should().Be(Path.Combine(AppContext.BaseDirectory, "Assets"));
    }

    [Fact]
    public void Windows_ExecutableExtension_IsExe()
    {
        new WindowsPlatformPaths().ExecutableExtension.Should().Be(".exe");
    }

    // ── MacPlatformPaths ─────────────────────────────────────────────────────────

    [Fact]
    public void Mac_ConfigAndDataDir_AreUnderApplicationSupport()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expected = Path.Combine(home, "Library", "Application Support", "YtDownloader");

        var paths = new MacPlatformPaths();
        paths.ConfigDir.Should().Be(expected);
        paths.DataDir.Should().Be(expected);
    }

    [Fact]
    public void Mac_ToolsDir_IsUnderCaches()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expected = Path.Combine(home, "Library", "Caches", "YtDownloader", "tools");

        new MacPlatformPaths().ToolsDir.Should().Be(expected);
    }

    [Fact]
    public void Mac_ExecutableExtension_IsEmpty()
    {
        new MacPlatformPaths().ExecutableExtension.Should().BeEmpty();
    }

    // ── LinuxPlatformPaths ───────────────────────────────────────────────────────

    [Fact]
    public void Linux_ConfigDir_DefaultsUnderDotConfig_WhenXdgConfigHomeUnset()
    {
        WithEnvironmentVariable("XDG_CONFIG_HOME", null, () =>
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            new LinuxPlatformPaths().ConfigDir.Should().Be(Path.Combine(home, ".config", "YtDownloader"));
        });
    }

    [Fact]
    public void Linux_ConfigDir_RespectsXdgConfigHomeOverride()
    {
        WithEnvironmentVariable("XDG_CONFIG_HOME", "/custom/config", () =>
        {
            new LinuxPlatformPaths().ConfigDir.Should().Be(Path.Combine("/custom/config", "YtDownloader"));
        });
    }

    [Fact]
    public void Linux_DataDir_DefaultsUnderLocalShare_WhenXdgDataHomeUnset()
    {
        WithEnvironmentVariable("XDG_DATA_HOME", null, () =>
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            new LinuxPlatformPaths().DataDir.Should().Be(Path.Combine(home, ".local", "share", "YtDownloader"));
        });
    }

    [Fact]
    public void Linux_ToolsDir_DefaultsUnderCache_WhenXdgCacheHomeUnset()
    {
        WithEnvironmentVariable("XDG_CACHE_HOME", null, () =>
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            new LinuxPlatformPaths().ToolsDir.Should().Be(Path.Combine(home, ".cache", "YtDownloader", "tools"));
        });
    }

    [Fact]
    public void Linux_ExecutableExtension_IsEmpty()
    {
        new LinuxPlatformPaths().ExecutableExtension.Should().BeEmpty();
    }

    private static void WithEnvironmentVariable(string name, string? value, Action test)
    {
        var original = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            test();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, original);
        }
    }
}
