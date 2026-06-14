using FluentAssertions;
using YtDownloader.Models;
using YtDownloader.Services;
using YtDownloader.ViewModels;

namespace YtDownloader.Core.Tests;

/// <summary>
/// Tests for <see cref="HistoryViewModel"/>. The view model wraps the
/// <see cref="HistoryService"/> and <see cref="AppSettings"/> singletons, so
/// tests add/remove items via <see cref="HistoryService.Instance"/> directly
/// (bypassing <see cref="HistoryService.Add"/>'s persistence) and restore any
/// settings they change.
/// </summary>
public class HistoryViewModelTests
{
    [Fact]
    public void HistoryItems_ReturnsHistoryServiceItems()
    {
        var vm = new HistoryViewModel();

        vm.HistoryItems.Should().BeSameAs(HistoryService.Instance.Items);
    }

    [Fact]
    public void IsEmptyVisible_TogglesWithHistoryItemsCollection()
    {
        var vm = new HistoryViewModel();
        var history = HistoryService.Instance;
        var initiallyEmpty = history.Items.Count == 0;

        vm.IsEmptyVisible.Should().Be(initiallyEmpty);

        var item = new DownloadHistoryItem
        {
            Title       = "Test Video",
            Url         = "https://example.com/video",
            OutputPath  = "/tmp",
            Format      = "MP4",
            Quality     = "Best available",
            CompletedAt = DateTime.Now,
        };

        try
        {
            history.Items.Add(item);
            vm.IsEmptyVisible.Should().BeFalse();
        }
        finally
        {
            history.Items.Remove(item);
        }

        vm.IsEmptyVisible.Should().Be(initiallyEmpty);
    }

    [Fact]
    public void IsDiagnosticsVisible_TracksShowDiagnosticsSetting()
    {
        var vm = new HistoryViewModel();
        var settings = AppSettings.Instance;
        var original = settings.ShowDiagnostics;

        try
        {
            settings.ShowDiagnostics = !original;
            vm.IsDiagnosticsVisible.Should().Be(!original);

            settings.ShowDiagnostics = original;
            vm.IsDiagnosticsVisible.Should().Be(original);
        }
        finally
        {
            settings.ShowDiagnostics = original;
        }
    }

    [Fact]
    public void DiagnosticInfo_IncludesStoragePathAndItemCount()
    {
        var vm = new HistoryViewModel();

        vm.DiagnosticInfo.Should().Contain(HistoryService.StoragePath);
        vm.DiagnosticInfo.Should().Contain($"Items loaded: {HistoryService.Instance.Items.Count}");
    }

    [Fact]
    public void ClearHistory_EmptiesItemsAndUpdatesIsEmptyVisible()
    {
        var vm = new HistoryViewModel();
        var history = HistoryService.Instance;
        history.Items.Add(new DownloadHistoryItem
        {
            Title       = "Test Video",
            Url         = "https://example.com/video",
            OutputPath  = "/tmp",
            Format      = "MP4",
            Quality     = "Best available",
            CompletedAt = DateTime.Now,
        });

        vm.ClearHistory();

        history.Items.Should().BeEmpty();
        vm.IsEmptyVisible.Should().BeTrue();
    }
}
