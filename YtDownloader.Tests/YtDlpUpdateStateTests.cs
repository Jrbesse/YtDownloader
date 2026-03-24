using FluentAssertions;
using YtDownloader.Services;

namespace YtDownloader.Tests;

/// <summary>
/// Tests for YtDlpUpdateState static flags and event firing.
/// Static state is reset before each test in the constructor and after in Dispose.
/// The [Collection] attribute ensures these run sequentially to avoid
/// cross-test contamination of the shared static state.
/// </summary>
[Collection("Sequential")]
public class YtDlpUpdateStateTests : IDisposable
{
    public YtDlpUpdateStateTests()
    {
        // Reset to known baseline before every test
        YtDlpUpdateState.SetUpdating(false);
        YtDlpUpdateState.SetSwappingFile(false);
    }

    public void Dispose()
    {
        YtDlpUpdateState.SetUpdating(false);
        YtDlpUpdateState.SetSwappingFile(false);
    }

    // ── Default state ──────────────────────────────────────────────────────────

    [Fact]
    public void IsUpdating_DefaultIsFalse()
    {
        YtDlpUpdateState.IsUpdating.Should().BeFalse();
    }

    [Fact]
    public void IsSwappingFile_DefaultIsFalse()
    {
        YtDlpUpdateState.IsSwappingFile.Should().BeFalse();
    }

    // ── SetUpdating ────────────────────────────────────────────────────────────

    [Fact]
    public void SetUpdating_True_IsUpdatingBecomesTrue()
    {
        YtDlpUpdateState.SetUpdating(true);
        YtDlpUpdateState.IsUpdating.Should().BeTrue();
    }

    [Fact]
    public void SetUpdating_False_IsUpdatingBecomesFalse()
    {
        YtDlpUpdateState.SetUpdating(true);
        YtDlpUpdateState.SetUpdating(false);
        YtDlpUpdateState.IsUpdating.Should().BeFalse();
    }

    // ── SetSwappingFile ────────────────────────────────────────────────────────

    [Fact]
    public void SetSwappingFile_True_IsSwappingFileBecomesTrue()
    {
        YtDlpUpdateState.SetSwappingFile(true);
        YtDlpUpdateState.IsSwappingFile.Should().BeTrue();
    }

    [Fact]
    public void SetSwappingFile_False_IsSwappingFileBecomesFalse()
    {
        YtDlpUpdateState.SetSwappingFile(true);
        YtDlpUpdateState.SetSwappingFile(false);
        YtDlpUpdateState.IsSwappingFile.Should().BeFalse();
    }

    // ── StateChanged event ─────────────────────────────────────────────────────

    [Fact]
    public void SetUpdating_RaisesStateChangedEvent()
    {
        var raised = false;
        YtDlpUpdateState.StateChanged += OnChanged;
        try
        {
            YtDlpUpdateState.SetUpdating(true);
            raised.Should().BeTrue();
        }
        finally
        {
            YtDlpUpdateState.StateChanged -= OnChanged;
        }

        void OnChanged() => raised = true;
    }

    [Fact]
    public void SetSwappingFile_RaisesStateChangedEvent()
    {
        var raised = false;
        YtDlpUpdateState.StateChanged += OnChanged;
        try
        {
            YtDlpUpdateState.SetSwappingFile(true);
            raised.Should().BeTrue();
        }
        finally
        {
            YtDlpUpdateState.StateChanged -= OnChanged;
        }

        void OnChanged() => raised = true;
    }

    [Fact]
    public void SetUpdating_NoSubscribers_DoesNotThrow()
    {
        // Ensure no subscribers — remove all by re-assigning null isn't possible on events,
        // but since we clean up in Dispose and tests run sequentially this is safe.
        var act = () => YtDlpUpdateState.SetUpdating(true);
        act.Should().NotThrow();
    }
}
