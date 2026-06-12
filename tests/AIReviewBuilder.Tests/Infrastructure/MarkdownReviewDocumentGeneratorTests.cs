using System.Collections.Immutable;
using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.ValueObjects;
using AIReviewBuilder.Infrastructure.Markdown;
using AIReviewBuilder.Infrastructure.Scanning;
using AIReviewBuilder.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIReviewBuilder.Tests.Infrastructure;

public sealed class MarkdownReviewDocumentGeneratorTests
{
    private static ReviewProfile Profile() =>
        new("security", "Security Review", ReviewProfileCategory.Security, "Audit for OWASP issues.");

    private static async Task<ImmutableArray<ScannedFile>> ScanAsync(TempWorkspace ws) =>
        (await new FileSystemRepositoryScanner(NullLogger<FileSystemRepositoryScanner>.Instance)
            .ScanAsync(ws.Root, FileSelectionCriteria.Default, ResourceLimits.Default, CancellationToken.None))
        .Files;

    [Fact]
    public async Task GenerateAsync_WritesDocumentWithHashAndEscaping()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("src/Program.cs", "var x = 1;\n");
        ImmutableArray<ScannedFile> files = await ScanAsync(ws);

        string outputPath = ws.Combine("review.md");
        var request = new ReviewDocumentRequest(
            ws.Root, "My|Repo", Profile(), files, outputPath, ResourceLimits.Default, includeFileContents: true);

        var generator = new MarkdownReviewDocumentGenerator(NullLogger<MarkdownReviewDocumentGenerator>.Instance);
        ReviewDocumentResult result = await generator.GenerateAsync(request, CancellationToken.None);

        Assert.True(File.Exists(outputPath));
        Assert.Equal(64, result.ContentSha256.Length); // SHA-256 hex
        Assert.NotEqual(0, result.FileCount);

        string content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("# Code Review: My\\|Repo", content, StringComparison.Ordinal); // pipe escaped
        Assert.Contains("## File Index", content, StringComparison.Ordinal);
        Assert.Contains("```csharp", content, StringComparison.Ordinal);
        Assert.Contains("var x = 1;", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_IsAtomic_NoTempFilesLeftBehind()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("a.cs", "class A {}\n");
        ImmutableArray<ScannedFile> files = await ScanAsync(ws);

        string outputPath = ws.Combine("out/review.md");
        var request = new ReviewDocumentRequest(
            ws.Root, "Repo", Profile(), files, outputPath, ResourceLimits.Default);

        await new MarkdownReviewDocumentGenerator(NullLogger<MarkdownReviewDocumentGenerator>.Instance)
            .GenerateAsync(request, CancellationToken.None);

        string[] leftovers = Directory.GetFiles(ws.Combine("out"), "*.tmp");
        Assert.Empty(leftovers);
    }
}
