using FluentAssertions;
using YtDownloader.Services;
using YtDownloader.ViewModels;

namespace YtDownloader.Core.Tests;

/// <summary>
/// Tests for <see cref="SettingsViewModel"/>. Most properties are
/// pass-throughs to the <see cref="AppSettings"/> singleton, so each test
/// restores the original value to avoid leaking state across tests.
/// </summary>
public class SettingsViewModelTests
{
    [Fact]
    public void DefaultState_ShowsCheckingForVersions()
    {
        var vm = new SettingsViewModel();

        vm.YtDlpVersion.Should().Be("Checking…");
        vm.FfmpegVersion.Should().Be("Checking…");
        vm.FfprobeVersion.Should().Be("Checking…");
        vm.AtomicParsleyVersion.Should().Be("Checking…");
        vm.UpdateStatus.Should().BeEmpty();
        vm.IsUpdating.Should().BeFalse();
    }

    [Fact]
    public void AvailableThemes_ContainsExpectedOptions()
    {
        var vm = new SettingsViewModel();

        vm.AvailableThemes.Should().BeEquivalentTo(new[] { "System", "Light", "Dark" });
    }

    [Fact]
    public void ShowDiagnostics_RoundTripsThroughAppSettings()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.ShowDiagnostics;

        try
        {
            vm.ShowDiagnostics = !original;

            vm.ShowDiagnostics.Should().Be(!original);
            AppSettings.Instance.ShowDiagnostics.Should().Be(!original);
        }
        finally
        {
            AppSettings.Instance.ShowDiagnostics = original;
        }
    }

    [Fact]
    public void ShowNotifications_RoundTripsThroughAppSettings()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.ShowNotifications;

        try
        {
            vm.ShowNotifications = !original;

            vm.ShowNotifications.Should().Be(!original);
            AppSettings.Instance.ShowNotifications.Should().Be(!original);
        }
        finally
        {
            AppSettings.Instance.ShowNotifications = original;
        }
    }

    [Fact]
    public void AutoCheckUpdates_RoundTripsThroughAppSettings()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.AutoCheckUpdates;

        try
        {
            vm.AutoCheckUpdates = !original;

            vm.AutoCheckUpdates.Should().Be(!original);
            AppSettings.Instance.AutoCheckUpdates.Should().Be(!original);
        }
        finally
        {
            AppSettings.Instance.AutoCheckUpdates = original;
        }
    }

    [Fact]
    public void RememberOutputFolder_RoundTripsThroughAppSettings()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.RememberOutputFolder;

        try
        {
            vm.RememberOutputFolder = !original;

            vm.RememberOutputFolder.Should().Be(!original);
            AppSettings.Instance.RememberOutputFolder.Should().Be(!original);
        }
        finally
        {
            AppSettings.Instance.RememberOutputFolder = original;
        }
    }

    [Fact]
    public void VerboseLogging_RoundTripsThroughAppSettings()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.VerboseLogging;

        try
        {
            vm.VerboseLogging = !original;

            vm.VerboseLogging.Should().Be(!original);
            AppSettings.Instance.VerboseLogging.Should().Be(!original);
        }
        finally
        {
            AppSettings.Instance.VerboseLogging = original;
        }
    }

    [Fact]
    public void SelectedTheme_RoundTripsThroughAppSettings()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.Theme;
        var newTheme = original == "Dark" ? "Light" : "Dark";

        try
        {
            vm.SelectedTheme = newTheme;

            vm.SelectedTheme.Should().Be(newTheme);
            AppSettings.Instance.Theme.Should().Be(newTheme);
        }
        finally
        {
            AppSettings.Instance.Theme = original;
        }
    }

    [Fact]
    public void IsAdvancedMode_RaisesAdvancedModeChangedEvent_WhenValueChanges()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.IsAdvancedMode;
        bool? raised = null;
        void Handler(bool value) => raised = value;

        SettingsViewModel.AdvancedModeChanged += Handler;
        try
        {
            vm.IsAdvancedMode = !original;

            raised.Should().Be(!original);
            vm.IsAdvancedMode.Should().Be(!original);
        }
        finally
        {
            SettingsViewModel.AdvancedModeChanged -= Handler;
            AppSettings.Instance.IsAdvancedMode = original;
        }
    }

    [Fact]
    public void IsAdvancedMode_DoesNotRaiseEvent_WhenValueUnchanged()
    {
        var vm = new SettingsViewModel();
        var original = AppSettings.Instance.IsAdvancedMode;
        var raiseCount = 0;
        void Handler(bool value) => raiseCount++;

        SettingsViewModel.AdvancedModeChanged += Handler;
        try
        {
            vm.IsAdvancedMode = original;

            raiseCount.Should().Be(0);
        }
        finally
        {
            SettingsViewModel.AdvancedModeChanged -= Handler;
            AppSettings.Instance.IsAdvancedMode = original;
        }
    }
}
