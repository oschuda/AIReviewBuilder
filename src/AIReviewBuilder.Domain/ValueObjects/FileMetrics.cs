using AIReviewBuilder.Domain.Common;

namespace AIReviewBuilder.Domain.ValueObjects;

/// <summary>
/// Immutable metric bundle describing a single text artifact: byte size, line count,
/// and an approximate token count (GLOBAL ENGINEERING STANDARD §8 immutable DTOs).
/// </summary>
public readonly record struct FileMetrics
{
    public FileMetrics(long byteCount, int lineCount, int approximateTokenCount)
    {
        Guard.NotNegative(byteCount);
        Guard.NotNegative(lineCount);
        Guard.NotNegative(approximateTokenCount);

        ByteCount = byteCount;
        LineCount = lineCount;
        ApproximateTokenCount = approximateTokenCount;
    }

    /// <summary>Size of the content in bytes.</summary>
    public long ByteCount { get; }

    /// <summary>Number of lines in the content.</summary>
    public int LineCount { get; }

    /// <summary>Heuristic, deterministic approximation of LLM tokens.</summary>
    public int ApproximateTokenCount { get; }

    public static FileMetrics Empty => new(0, 0, 0);

    /// <summary>Aggregates two metric bundles, saturating at the type maxima to avoid overflow.</summary>
    public FileMetrics Add(FileMetrics other)
    {
        long bytes = ByteCount + other.ByteCount;
        long lines = (long)LineCount + other.LineCount;
        long tokens = (long)ApproximateTokenCount + other.ApproximateTokenCount;

        return new FileMetrics(
            bytes,
            (int)Math.Min(lines, int.MaxValue),
            (int)Math.Min(tokens, int.MaxValue));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutability/determinism, §25 invariants.
// - Architectural & Concurrency Risks: None. readonly record struct is thread-safe.
// - Security & Trust-Boundary Risks: None; carries only numeric metadata (§17).
// - Determinism & Encoding Risks: None; pure arithmetic.
// - Resource Exhaustion Risks: Add() saturates int fields to avoid overflow; byte
//   totals use Int64 (sufficient for the 100 MB cumulative ceiling).
// - Explicit Assumptions Made: Per-file line/token counts fit in Int32 given the
//   5 MB single-file ceiling.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
