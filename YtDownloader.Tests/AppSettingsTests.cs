using FluentAssertions;
using System.Text.Json;
using YtDownloader.Services;

namespace YtDownloader.Tests;

/// <summary>
/// Tests for AppSettings persistence, defaults, and round-trip behavior.
/// Each test uses the internal constructor with a unique temp path
/// to fully isolate from the real %LocalAppData% settings file.
/// </summary>
public class AppSettingsTests : IDisposable
{
    private readonly string _tempPath;

    public AppSettingsTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"yt-settings-test-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath))            File.Delete(_tempPath);
        if (File.Exists(_tempPath + ".tmp"))   File.Delete(_tempPath + ".tmp");
    }

    private AppSettings CreateSettings() => new(_tempPath);

    // ── Defaults ───────────────────────────────────────────────────────────────

    [Fact]
    public void DefaultShowDiagnostics_IsFalse()
    {
        CreateSettings().ShowDiagnostics.Should().BeFalse();
    }

    [Fact]
    public void DefaultTheme_IsSystem()
    {
        CreateSettings().Theme.Should().Be("System");
    }

    [Fact]
    public void DefaultIsAdvancedMode_IsFalse()
    {
        CreateSettings().IsAdvancedMode.Should().BeFalse();
    }

    [Fact]
    public void DefaultShowNotifications_IsTrue()
    {
        CreateSettings().ShowNotifications.Should().BeTrue();
    }

    [Fact]
    public void DefaultAutoCheckUpdates_IsTrue()
    {
        CreateSettings().AutoCheckUpdates.Should().BeTrue();
    }

    [Fact]
    public void DefaultRememberOutputFolder_IsTrue()
    {
        CreateSettings().RememberOutputFolder.Should().BeTrue();
    }

    [Fact]
    public void DefaultLastOutputFolder_IsEmptyString()
    {
        CreateSettings().LastOutputFolder.Should().BeEmpty();
    }

    [Fact]
    public void DefaultVerboseLogging_IsFalse()
    {
        CreateSettings().VerboseLogging.Should().BeFalse();
    }

    // ── Round-trip: save then load ─────────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_PreservesTheme()
    {
        var s1 = CreateSettings();
        s1.Theme = "Dark";

        var s2 = CreateSettings();
        s2.Theme.Should().Be("Dark");
    }

    [Fact]
    public void SaveThenLoad_PreservesIsAdvancedMode()
    {
        var s1 = CreateSettings();
        s1.IsAdvancedMode = true;

        var s2 = CreateSettings();
        s2.IsAdvancedMode.Should().BeTrue();
    }

    [Fact]
    public void SaveThenLoad_PreservesVerboseLogging()
    {
        var s1 = CreateSettings();
        s1.VerboseLogging = true;

        var s2 = CreateSettings();
        s2.VerboseLogging.Should().BeTrue();
    }

    [Fact]
    public void SaveThenLoad_PreservesLastOutputFolder()
    {
        var s1 = CreateSettings();
        s1.LastOutputFolder = @"C:\My Downloads";

        var s2 = CreateSettings();
        s2.LastOutputFolder.Should().Be(@"C:\My Downloads");
    }

    [Fact]
    public void SaveThenLoad_PreservesShowNotifications()
    {
        var s1 = CreateSettings();
        s1.ShowNotifications = false;

        var s2 = CreateSettings();
        s2.ShowNotifications.Should().BeFalse();
    }

    [Fact]
    public void SaveThenLoad_PreservesAllSettings()
    {
        var s1 = CreateSettings();
        s1.Theme               = "Light";
        s1.IsAdvancedMode      = true;
        s1.ShowNotifications   = false;
        s1.AutoCheckUpdates    = false;
        s1.RememberOutputFolder = false;
        s1.LastOutputFolder    = @"D:\Videos";
        s1.VerboseLogging      = true;
        s1.ShowDiagnostics     = true;

        var s2 = CreateSettings();
        s2.Theme.Should().Be("Light");
        s2.IsAdvancedMode.Should().BeTrue();
        s2.ShowNotifications.Should().BeFalse();
        s2.AutoCheckUpdates.Should().BeFalse();
        s2.RememberOutputFolder.Should().BeFalse();
        s2.LastOutputFolder.Should().Be(@"D:\Videos");
        s2.VerboseLogging.Should().BeTrue();
        s2.ShowDiagnostics.Should().BeTrue();
    }

    // ── Load edge cases ────────────────────────────────────────────────────────

    [Fact]
    public void Load_CorruptJson_DoesNotThrow_FallsBackToDefaults()
    {
        File.WriteAllText(_tempPath, "NOT VALID JSON }{");
        var act = () => CreateSettings();
        act.Should().NotThrow();
        CreateSettings().Theme.Should().Be("System");
    }

    [Fact]
    public void Load_EmptyFile_DoesNotThrow_FallsBackToDefaults()
    {
        File.WriteAllText(_tempPath, string.Empty);
        var act = () => CreateSettings();
        act.Should().NotThrow();
        CreateSettings().VerboseLogging.Should().BeFalse();
    }

    [Fact]
    public void Load_PartialJson_MissingKeys_UsesDefaults()
    {
        // Only Theme is present; all other keys missing
        File.WriteAllText(_tempPath, "{\"Theme\":\"Dark\"}");
        var s = CreateSettings();
        s.Theme.Should().Be("Dark");
        s.VerboseLogging.Should().BeFalse();   // default
        s.ShowNotifications.Should().BeTrue(); // default
    }

    [Fact]
    public void Load_NullTheme_FallsBackToSystem()
    {
        File.WriteAllText(_tempPath, "{\"Theme\":null}");
        CreateSettings().Theme.Should().Be("System");
    }

    [Fact]
    public void Load_NullLastOutputFolder_FallsBackToEmptyString()
    {
        File.WriteAllText(_tempPath, "{\"LastOutputFolder\":null}");
        CreateSettings().LastOutputFolder.Should().BeEmpty();
    }

    // ── Save safety ────────────────────────────────────────────────────────────

    [Fact]
    public void Save_CreatesDirectory_IfMissing()
    {
        var nestedPath = Path.Combine(Path.GetTempPath(), $"yt-test-{Guid.NewGuid():N}", "sub", "settings.json");
        try
        {
            var s = new AppSettings(nestedPath);
            s.Theme = "Dark"; // triggers Save()
            File.Exists(nestedPath).Should().BeTrue();
        }
        finally
        {
            var dir = Path.GetDirectoryName(Path.GetDirectoryName(nestedPath))!;
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Save_NoTempFileRemains_AfterSuccessfulSave()
    {
        var s = CreateSettings();
        s.Theme = "Light";
        File.Exists(_tempPath + ".tmp").Should().BeFalse();
    }

    // ── INPC / observable behavior ─────────────────────────────────────────────

    [Fact]
    public void PropertyChanged_RaisedWhenThemeChanges()
    {
        var s = CreateSettings();
        var raised = false;
        s.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(s.Theme)) raised = true;
        };

        s.Theme = "Dark";
        raised.Should().BeTrue();
    }

    [Fact]
    public void Load_DoesNotTriggerSave_NoSpuriousTempFile()
    {
        // Before calling CreateSettings(), temp file must not exist
        File.Exists(_tempPath + ".tmp").Should().BeFalse();
        // Constructing settings (which calls Load) should not write a .tmp file
        _ = CreateSettings();
        File.Exists(_tempPath + ".tmp").Should().BeFalse();
    }
}
