using AIReviewBuilder.Application.Abstractions;
using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Application.Services;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Infrastructure.Markdown;
using AIReviewBuilder.Infrastructure.Packaging;
using AIReviewBuilder.Infrastructure.Scanning;
using AIReviewBuilder.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIReviewBuilder.Tests.Application;

public sealed class ReviewOrchestratorTests
{
    private static IReviewOrchestrator CreateOrchestrator() =>
        new ReviewOrchestrator(
            new FileSystemRepositoryScanner(NullLogger<FileSystemRepositoryScanner>.Instance),
            new MarkdownReviewDocumentGenerator(NullLogger<MarkdownReviewDocumentGenerator>.Instance),
            new ZipSourceBundlePacker(NullLogger<ZipSourceBundlePacker>.Instance),
            NullLogger<ReviewOrchestrator>.Instance);

    [Fact]
    public async Task BuildAsync_GeneratesMarkdownAndBundle_EndToEnd()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("src/Program.cs", "class Program { static void Main() {} }\n");
        ws.WriteFile("README.md", "# Demo\n");

        string outputDir = ws.Combine("output");
        var request = new ReviewBuildRequest(
            ws.Root,
            "Demo",
            new ReviewProfile("general", "General", ReviewProfileCategory.General, "Review this."),
            outputDir,
            generateMarkdown: true,
            generateSourceBundle: true);

        ReviewBuildResult result = await CreateOrchestrator().BuildAsync(request, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.CorrelationId));
        Assert.Equal(2, result.Scan.FileCount);

        Assert.NotNull(result.Document);
        Assert.True(File.Exists(Path.Combine(outputDir, ReviewBuildRequest.DefaultReviewFileName)));

        Assert.NotNull(result.Bundle);
        Assert.True(File.Exists(Path.Combine(outputDir, ReviewBuildRequest.DefaultBundleFileName)));
    }

    [Fact]
    public async Task BuildAsync_Cancellation_Throws()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("a.cs", "class A {}\n");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var request = new ReviewBuildRequest(
            ws.Root,
            "Demo",
            new ReviewProfile("general", "General", ReviewProfileCategory.General, "Review this."),
            ws.Combine("out"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateOrchestrator().BuildAsync(request, cts.Token));
    }
}
