using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// Immutable outcome of a successful review-document generation, including the SHA-256
/// of the finalised file (computed after all writes are flushed, APPLICATION SPEC §21).
/// </summary>
public sealed class ReviewDocumentResult
{
    public ReviewDocumentResult(
        string outputFilePath,
        int fileCount,
        FileMetrics aggregate,
        string contentSha256,
        TimeSpan duration)
    {
        OutputFilePath = Guard.NotNullOrWhiteSpace(outputFilePath);
        FileCount = (int)Guard.NotNegative(fileCount);
        Aggregate = aggregate;
        ContentSha256 = Guard.NotNullOrWhiteSpace(contentSha256);
        Duration = duration;
    }

    /// <summary>Absolute path of the written review.md.</summary>
    public string OutputFilePath { get; }

    /// <summary>Number of files included.</summary>
    public int FileCount { get; }

    /// <summary>Aggregate metrics across all included files.</summary>
    public FileMetrics Aggregate { get; }

    /// <summary>Lowercase hexadecimal SHA-256 of the finalised file content.</summary>
    public string ContentSha256 { get; }

    /// <summary>Wall-clock duration of the generation.</summary>
    public TimeSpan Duration { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutable DTO; §21/§27 integrity hash; §34 SHA-256.
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: None; metadata only (§17).
// - Determinism & Encoding Risks: SHA-256 is hex lowercase, culture-independent.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Hash is computed on the complete finalised content.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
