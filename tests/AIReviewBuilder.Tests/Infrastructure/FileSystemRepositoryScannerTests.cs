using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.ValueObjects;
using AIReviewBuilder.Infrastructure.Scanning;
using AIReviewBuilder.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIReviewBuilder.Tests.Infrastructure;

public sealed class FileSystemRepositoryScannerTests
{
    private static FileSystemRepositoryScanner CreateScanner() =>
        new(NullLogger<FileSystemRepositoryScanner>.Instance);

    [Fact]
    public async Task ScanAsync_AppliesIncludeExcludeAndSortsDeterministically()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("src/A.cs", "class A {}\n");
        ws.WriteFile("README.md", "# Title\n");
        ws.WriteFile("data.json", "{}\n");
        ws.WriteFile("bin/Debug/Excluded.cs", "class X {}\n");
        ws.WriteFile("notes.txt", "ignored extension\n");

        var result = await CreateScanner().ScanAsync(
            ws.Root, FileSelectionCriteria.Default, ResourceLimits.Default, CancellationToken.None);

        Assert.Equal(
            new[] { "README.md", "data.json", "src/A.cs" },
            result.Files.Select(f => f.RelativePath).ToArray());
        Assert.Equal(3, result.FileCount);
        Assert.True(result.Aggregate.ByteCount > 0);
        Assert.True(result.Aggregate.LineCount >= 3);
    }

    [Fact]
    public async Task ScanAsync_MissingDirectory_Throws()
    {
        using var ws = new TempWorkspace();
        await Assert.ThrowsAsync<InfrastructureException>(() =>
            CreateScanner().ScanAsync(
                ws.Combine("does-not-exist"),
                FileSelectionCriteria.Default,
                ResourceLimits.Default,
                CancellationToken.None));
    }

    [Fact]
    public async Task ScanAsync_FileCountCeiling_Throws()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("a.cs", "x\n");
        ws.WriteFile("b.cs", "x\n");

        var limits = new ResourceLimits(maxFileCount: 1, maxSingleFileBytes: 5_000_000, maxCumulativeBytes: 100_000_000);

        await Assert.ThrowsAsync<ResourceLimitExceededException>(() =>
            CreateScanner().ScanAsync(ws.Root, FileSelectionCriteria.Default, limits, CancellationToken.None));
    }

    [Fact]
    public async Task ScanAsync_SingleFileCeiling_Throws()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("big.cs", new string('x', 64));

        var limits = new ResourceLimits(maxFileCount: 5_000, maxSingleFileBytes: 8, maxCumulativeBytes: 100_000_000);

        await Assert.ThrowsAsync<ResourceLimitExceededException>(() =>
            CreateScanner().ScanAsync(ws.Root, FileSelectionCriteria.Default, limits, CancellationToken.None));
    }

    [Fact]
    public async Task ScanAsync_CumulativeCeiling_Throws()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("a.cs", "hello!");  // 6 bytes
        ws.WriteFile("b.cs", "hello!");  // 6 bytes -> cumulative 12 > 10

        var limits = new ResourceLimits(maxFileCount: 5_000, maxSingleFileBytes: 10, maxCumulativeBytes: 10);

        await Assert.ThrowsAsync<ResourceLimitExceededException>(() =>
            CreateScanner().ScanAsync(ws.Root, FileSelectionCriteria.Default, limits, CancellationToken.None));
    }

    [Fact]
    public async Task ScanAsync_IgnoresSymlinkedDirectories()
    {
        using var ws = new TempWorkspace();
        using var external = new TempWorkspace();
        external.WriteFile("leaked.cs", "class Secret {}\n");

        string linkPath = ws.Combine("linked");
        try
        {
            Directory.CreateSymbolicLink(linkPath, external.Root);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // Symlink creation not permitted in this environment; skip.
        }

        ws.WriteFile("real.cs", "class Real {}\n");

        var result = await CreateScanner().ScanAsync(
            ws.Root, FileSelectionCriteria.Default, ResourceLimits.Default, CancellationToken.None);

        Assert.Contains(result.Files, f => f.RelativePath == "real.cs");
        Assert.DoesNotContain(result.Files, f => f.RelativePath.Contains("leaked", StringComparison.Ordinal));
    }
}
