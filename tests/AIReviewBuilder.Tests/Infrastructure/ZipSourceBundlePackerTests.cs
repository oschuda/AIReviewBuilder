using System.Collections.Immutable;
using System.IO.Compression;
using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Domain.ValueObjects;
using AIReviewBuilder.Infrastructure.Packaging;
using AIReviewBuilder.Infrastructure.Scanning;
using AIReviewBuilder.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIReviewBuilder.Tests.Infrastructure;

public sealed class ZipSourceBundlePackerTests
{
    private static async Task<ImmutableArray<AIReviewBuilder.Domain.Entities.ScannedFile>> ScanAsync(TempWorkspace ws) =>
        (await new FileSystemRepositoryScanner(NullLogger<FileSystemRepositoryScanner>.Instance)
            .ScanAsync(ws.Root, FileSelectionCriteria.Default, ResourceLimits.Default, CancellationToken.None))
        .Files;

    [Fact]
    public async Task PackAsync_ProducesArchiveWithExpectedEntries()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("src/A.cs", "class A {}\n");
        ws.WriteFile("README.md", "# Hi\n");
        ImmutableArray<AIReviewBuilder.Domain.Entities.ScannedFile> files = await ScanAsync(ws);

        string archivePath = ws.Combine("out/source-bundle.zip");
        var request = new SourceBundleRequest(ws.Root, files, archivePath, ResourceLimits.Default);

        var packer = new ZipSourceBundlePacker(NullLogger<ZipSourceBundlePacker>.Instance);
        SourceBundleResult result = await packer.PackAsync(request, CancellationToken.None);

        Assert.True(File.Exists(archivePath));
        Assert.Equal(2, result.FileCount);
        Assert.Equal(64, result.ArchiveSha256.Length);

        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        var entryNames = archive.Entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "README.md", "src/A.cs" }, entryNames);
    }

    [Fact]
    public async Task PackAsync_UsesForwardSlashEntryNames()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("a/b/c.cs", "class C {}\n");
        ImmutableArray<AIReviewBuilder.Domain.Entities.ScannedFile> files = await ScanAsync(ws);

        string archivePath = ws.Combine("bundle.zip");
        await new ZipSourceBundlePacker(NullLogger<ZipSourceBundlePacker>.Instance)
            .PackAsync(new SourceBundleRequest(ws.Root, files, archivePath, ResourceLimits.Default), CancellationToken.None);

        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        Assert.Contains(archive.Entries, e => e.FullName == "a/b/c.cs");
    }
}
