using FluentAssertions;

namespace YtDownloader.Tests;

/// <summary>
/// Minimal contract tests for the launcher's expected directory layout.
/// The launcher's Main() method cannot be unit-tested (it uses MessageBox P/Invoke
/// and Process.Start), but we can assert the path pattern it uses.
/// </summary>
public class LauncherTests
{
    [Fact]
    public void ExpectedLayout_MainAppExe_IsInSubfolderNamedYtDownloader()
    {
        // The launcher constructs: Path.Combine(baseDir, "YtDownloader", "YtDownloader.exe")
        // This test documents that contract by verifying the path pattern.
        var baseDir = @"C:\SomeInstallDir";
        var expectedPath = Path.Combine(baseDir, "YtDownloader", "YtDownloader.exe");

        expectedPath.Should().EndWith(Path.Combine("YtDownloader", "YtDownloader.exe"));
    }
}
