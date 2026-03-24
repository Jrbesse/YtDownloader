using FluentAssertions;
using System.Text.Json;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.Tests;

/// <summary>
/// Tests for HistoryService persistence and collection behavior.
/// Each test creates an isolated instance via the internal constructor
/// pointed at a unique temp file, cleaned up in Dispose.
/// </summary>
public class HistoryServiceTests : IDisposable
{
    private readonly string _tempPath;

    public HistoryServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"yt-history-test-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath))       File.Delete(_tempPath);
        if (File.Exists(_tempPath + ".tmp")) File.Delete(_tempPath + ".tmp");
    }

    private HistoryService CreateService() => new(_tempPath);

    private static DownloadHistoryItem SampleItem(string title = "Test Video") => new()
    {
        Title       = title,
        Url         = "https://www.youtube.com/watch?v=test",
        OutputPath  = @"C:\Downloads",
        Format      = "MP4",
        Quality     = "1080p",
        CompletedAt = new DateTime(2024, 6, 15, 12, 0, 0),
    };

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NoBacking_StartsWithEmptyItems()
    {
        CreateService().Items.Should().BeEmpty();
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_SingleItem_AppearsInCollection()
    {
        var svc = CreateService();
        svc.Add(SampleItem());
        svc.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Add_SingleItem_InsertedAtIndexZero()
    {
        var svc = CreateService();
        svc.Add(SampleItem("First"));
        svc.Items[0].Title.Should().Be("First");
    }

    [Fact]
    public void Add_TwoItems_MostRecentIsFirst()
    {
        var svc = CreateService();
        svc.Add(SampleItem("Older"));
        svc.Add(SampleItem("Newer"));
        svc.Items[0].Title.Should().Be("Newer");
        svc.Items[1].Title.Should().Be("Older");
    }

    [Fact]
    public void Add_SingleItem_PersistsToFile()
    {
        var svc = CreateService();
        svc.Add(SampleItem());
        File.Exists(_tempPath).Should().BeTrue();
    }

    [Fact]
    public void Add_SingleItem_ReloadedInstanceContainsItem()
    {
        var svc1 = CreateService();
        svc1.Add(SampleItem("Persisted"));

        var svc2 = CreateService();
        svc2.Items.Should().HaveCount(1);
        svc2.Items[0].Title.Should().Be("Persisted");
    }

    [Fact]
    public void Add_PreservesAllFields()
    {
        var original = SampleItem("Full Test");
        original.Url         = "https://example.com/watch?v=abc";
        original.OutputPath  = @"C:\My Videos";
        original.Format      = "MKV";
        original.Quality     = "720p";
        original.CompletedAt = new DateTime(2025, 3, 21, 9, 30, 0);

        var svc1 = CreateService();
        svc1.Add(original);

        var svc2 = CreateService();
        var loaded = svc2.Items[0];
        loaded.Title.Should().Be("Full Test");
        loaded.Url.Should().Be("https://example.com/watch?v=abc");
        loaded.OutputPath.Should().Be(@"C:\My Videos");
        loaded.Format.Should().Be("MKV");
        loaded.Quality.Should().Be("720p");
        loaded.CompletedAt.Should().Be(new DateTime(2025, 3, 21, 9, 30, 0));
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_EmptiesCollection()
    {
        var svc = CreateService();
        svc.Add(SampleItem());
        svc.Add(SampleItem());
        svc.Clear();
        svc.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_PersistsEmptyListToFile()
    {
        var svc = CreateService();
        svc.Add(SampleItem());
        svc.Clear();

        var svc2 = CreateService();
        svc2.Items.Should().BeEmpty();
    }

    // ── Load edge cases ───────────────────────────────────────────────────────

    [Fact]
    public void Load_ValidJson_RestoresItems()
    {
        var items = new[]
        {
            new { Title = "A", Url = "u1", OutputPath = "p1", Format = "MP4", Quality = "1080p", CompletedAt = DateTime.Now },
            new { Title = "B", Url = "u2", OutputPath = "p2", Format = "MP3", Quality = "",      CompletedAt = DateTime.Now },
        };
        Directory.CreateDirectory(Path.GetDirectoryName(_tempPath)!);
        File.WriteAllText(_tempPath, JsonSerializer.Serialize(items));

        var svc = CreateService();
        svc.Items.Should().HaveCount(2);
        svc.Items.Any(i => i.Title == "A").Should().BeTrue();
        svc.Items.Any(i => i.Title == "B").Should().BeTrue();
    }

    [Fact]
    public void Load_CorruptJson_DoesNotThrow_StartsEmpty()
    {
        File.WriteAllText(_tempPath, "NOT VALID JSON {{{{");
        HistoryService? svc = null;
        var act = () => { svc = CreateService(); };
        act.Should().NotThrow();
        svc!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Load_EmptyFile_StartsEmpty()
    {
        File.WriteAllText(_tempPath, string.Empty);
        CreateService().Items.Should().BeEmpty();
    }

    [Fact]
    public void Load_NullItemInArray_SkipsNullEntry()
    {
        File.WriteAllText(_tempPath, "[null, {\"Title\":\"Real\",\"Url\":\"\",\"OutputPath\":\"\",\"Format\":\"\",\"Quality\":\"\"}]");
        var svc = CreateService();
        svc.Items.Should().HaveCount(1);
        svc.Items[0].Title.Should().Be("Real");
    }

    [Fact]
    public void Load_NullTitle_DefaultsToEmptyString()
    {
        File.WriteAllText(_tempPath, "[{\"Title\":null,\"Url\":\"u\",\"OutputPath\":\"p\",\"Format\":\"MP4\",\"Quality\":\"720p\"}]");
        var svc = CreateService();
        svc.Items[0].Title.Should().BeEmpty();
    }

    [Fact]
    public void Load_NullUrl_DefaultsToEmptyString()
    {
        File.WriteAllText(_tempPath, "[{\"Title\":\"T\",\"Url\":null,\"OutputPath\":\"p\",\"Format\":\"MP4\",\"Quality\":\"720p\"}]");
        var svc = CreateService();
        svc.Items[0].Url.Should().BeEmpty();
    }

    [Fact]
    public void Load_NullOutputPath_DefaultsToEmptyString()
    {
        File.WriteAllText(_tempPath, "[{\"Title\":\"T\",\"Url\":\"u\",\"OutputPath\":null,\"Format\":\"MP4\",\"Quality\":\"720p\"}]");
        var svc = CreateService();
        svc.Items[0].OutputPath.Should().BeEmpty();
    }

    // ── Save safety ───────────────────────────────────────────────────────────

    [Fact]
    public void Save_CreatesDirectory_IfMissing()
    {
        var nestedPath = Path.Combine(Path.GetTempPath(), $"yt-test-{Guid.NewGuid():N}", "sub", "history.json");
        try
        {
            var svc = new HistoryService(nestedPath);
            svc.Add(SampleItem());
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
        var svc = CreateService();
        svc.Add(SampleItem());
        File.Exists(_tempPath + ".tmp").Should().BeFalse();
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    [Fact]
    public void LastLoadError_IsNull_AfterSuccessfulLoad()
    {
        var svc = CreateService();
        svc.Add(SampleItem());

        var svc2 = CreateService();
        svc2.LastLoadError.Should().BeNull();
    }

    [Fact]
    public void LastSaveError_IsNull_AfterSuccessfulSave()
    {
        var svc = CreateService();
        svc.Add(SampleItem());
        svc.LastSaveError.Should().BeNull();
    }

    [Fact]
    public void LastLoadInfo_ContainsResolvedPath()
    {
        var svc = CreateService();
        svc.LastLoadInfo.Should().Contain("Resolved path:");
    }
}
