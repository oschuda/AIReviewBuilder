using System.Globalization;
using System.Text;
using AIReviewBuilder.Application.Abstractions;
using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.Services;
using AIReviewBuilder.Infrastructure.IO;
using Microsoft.Extensions.Logging;

namespace AIReviewBuilder.Infrastructure.Markdown;

/// <summary>
/// Compiles selected files into an LLM-optimised <c>review.md</c>. File paths and metadata
/// are escaped via <see cref="MarkdownSanitizer"/> (Markdown-injection mitigation) and file
/// bodies are wrapped in dynamically sized fences so content cannot break out. The file is
/// produced atomically with an integrity hash (§21) and written as UTF-8 without BOM with
/// deterministic '\n' line endings (§8).
/// </summary>
public sealed partial class MarkdownReviewDocumentGenerator : IReviewDocumentGenerator
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private readonly ILogger<MarkdownReviewDocumentGenerator> _logger;

    public MarkdownReviewDocumentGenerator(ILogger<MarkdownReviewDocumentGenerator> logger)
    {
        _logger = Guard.NotNull(logger);
    }

    public async Task<ReviewDocumentResult> GenerateAsync(ReviewDocumentRequest request, CancellationToken cancellationToken)
    {
        Guard.NotNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(request.RepositoryRootPath));
        var start = System.Diagnostics.Stopwatch.StartNew();

        var aggregate = Domain.ValueObjects.FileMetrics.Empty;
        long cumulativeBytes = 0;
        int includedCount = 0;

        string hash = await AtomicFileWriter.WriteAsync(
            request.OutputFilePath,
            async (stream, ct) =>
            {
                await using var writer = new StreamWriter(stream, Utf8NoBom, bufferSize: 1 << 16, leaveOpen: true)
                {
                    NewLine = "\n",
                    AutoFlush = false,
                };

                await WriteHeaderAsync(writer, request, ct).ConfigureAwait(false);
                await WriteFileIndexAsync(writer, request, ct).ConfigureAwait(false);

                if (request.IncludeFileContents)
                {
                    foreach (var file in request.Files)
                    {
                        ct.ThrowIfCancellationRequested();

                        string absolutePath = PathBoundaryGuard.ResolveWithinRoot(root, file.RelativePath);
                        var info = new FileInfo(absolutePath);
                        if (!info.Exists || FileSystemSafety.IsReparsePoint(info))
                        {
                            continue;
                        }

                        request.Limits.EnsureSingleFileWithinLimit(info.Length);

                        includedCount++;
                        request.Limits.EnsureFileCountWithinLimit(includedCount);

                        cumulativeBytes += info.Length;
                        request.Limits.EnsureCumulativeWithinLimit(cumulativeBytes);

                        string content = await ReadTextAsync(absolutePath, ct).ConfigureAwait(false);
                        aggregate = aggregate.Add(TokenEstimator.EstimateMetrics(content, info.Length));

                        await WriteFileSectionAsync(writer, file.RelativePath, content, ct).ConfigureAwait(false);
                    }
                }

                await writer.FlushAsync(ct).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        start.Stop();
        LogGenerated(includedCount, aggregate.ByteCount);

        int reportedCount = request.IncludeFileContents ? includedCount : request.Files.Length;
        return new ReviewDocumentResult(
            Path.GetFullPath(request.OutputFilePath),
            reportedCount,
            aggregate,
            hash,
            start.Elapsed);
    }

    private static async Task WriteHeaderAsync(TextWriter writer, ReviewDocumentRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        await writer.WriteLineAsync($"# Code Review: {MarkdownSanitizer.EscapeInline(request.RepositoryName)}").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync($"- **Generated (UTC):** {timestamp}").ConfigureAwait(false);
        await writer.WriteLineAsync($"- **Profile:** {MarkdownSanitizer.EscapeInline(request.Profile.Name)} ({request.Profile.Category})").ConfigureAwait(false);
        await writer.WriteLineAsync($"- **Files:** {request.Files.Length.ToString(CultureInfo.InvariantCulture)}").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync("## Review Instructions").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);

        string fence = MarkdownSanitizer.SelectCodeFence(request.Profile.Prompt);
        await writer.WriteLineAsync(fence).ConfigureAwait(false);
        await writer.WriteLineAsync(NormalizeNewlines(request.Profile.Prompt)).ConfigureAwait(false);
        await writer.WriteLineAsync(fence).ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
    }

    private static async Task WriteFileIndexAsync(TextWriter writer, ReviewDocumentRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await writer.WriteLineAsync("## File Index").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync("| File | Lines | Bytes | ~Tokens |").ConfigureAwait(false);
        await writer.WriteLineAsync("| --- | ---: | ---: | ---: |").ConfigureAwait(false);

        foreach (var file in request.Files)
        {
            ct.ThrowIfCancellationRequested();
            string path = MarkdownSanitizer.EscapeInline(file.RelativePath);
            await writer.WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"| {path} | {file.Metrics.LineCount} | {file.Metrics.ByteCount} | {file.Metrics.ApproximateTokenCount} |"))
                .ConfigureAwait(false);
        }

        await writer.WriteLineAsync().ConfigureAwait(false);
    }

    private static async Task WriteFileSectionAsync(TextWriter writer, string relativePath, string content, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await writer.WriteLineAsync($"### {MarkdownSanitizer.EscapeInline(relativePath)}").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);

        string fence = MarkdownSanitizer.SelectCodeFence(content);
        string language = LanguageHint(relativePath);
        await writer.WriteLineAsync(fence + language).ConfigureAwait(false);
        await writer.WriteLineAsync(NormalizeNewlines(content)).ConfigureAwait(false);
        await writer.WriteLineAsync(fence).ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
    }

    private static async Task<string> ReadTextAsync(string absolutePath, CancellationToken ct)
    {
        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(absolutePath, ct).ConfigureAwait(false);
            ReadOnlySpan<byte> span = bytes;
            if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
            {
                span = span[3..];
            }

            return Utf8NoBom.GetString(span);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InfrastructureException("Failed to read a file while generating the review document.", ex);
        }
    }

    private static string NormalizeNewlines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string LanguageHint(string relativePath)
    {
        string ext = Path.GetExtension(relativePath).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "csharp",
            ".py" => "python",
            ".md" => "markdown",
            ".json" => "json",
            ".yaml" or ".yml" => "yaml",
            _ => string.Empty,
        };
    }

    [LoggerMessage(EventId = 2200, Level = LogLevel.Information,
        Message = "review.md generated for {FileCount} files ({ByteCount} content bytes).")]
    private partial void LogGenerated(int fileCount, long byteCount);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — Markdown-injection mitigation (escape + dynamic fences);
//   §21 atomic generation with post-flush hash; §8 UTF-8 no BOM, '\n', InvariantCulture,
//   UTC timestamp; §9 per-file re-validation + symlink skip; §10 limits re-enforced;
//   §17 metadata-only logging; §21/§24 async with ConfigureAwait(false) + token.
// - Architectural & Concurrency Risks: Local accumulators only; no shared mutable
//   state (§12). StreamWriter leaveOpen:true so AtomicFileWriter controls the stream.
// - Security & Trust-Boundary Risks: Repository name, profile name, and every relative
//   path are escaped; file bodies fenced with a fence longer than any internal backtick
//   run; paths resolved within root before reading (traversal-safe).
// - Determinism & Encoding Risks: Newlines normalised; numbers formatted invariantly.
// - Resource Exhaustion Risks: Size verified before each read; one file in memory at a
//   time; cumulative/count ceilings enforced.
// - Explicit Assumptions Made: DateTime.UtcNow is the documented environment dependency
//   (timestamps are UTC); content is decoded as UTF-8 with replacement for robustness.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Extremely large aggregate documents are bounded by the
//   100 MB cumulative ceiling; downstream LLM context limits are the caller's concern.
// ─────────────────────────────────────────────────────────────────────────────
