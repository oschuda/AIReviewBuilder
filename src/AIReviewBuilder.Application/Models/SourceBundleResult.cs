using AIReviewBuilder.Domain.Common;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// Immutable outcome of a successful source-bundle pack, including the SHA-256 of the
/// finalised archive (computed after flush, APPLICATION SPEC §21).
/// </summary>
public sealed class SourceBundleResult
{
    public SourceBundleResult(
        string outputArchivePath,
        int fileCount,
        long uncompressedByteCount,
        string archiveSha256,
        TimeSpan duration)
    {
        OutputArchivePath = Guard.NotNullOrWhiteSpace(outputArchivePath);
        FileCount = (int)Guard.NotNegative(fileCount);
        UncompressedByteCount = Guard.NotNegative(uncompressedByteCount);
        ArchiveSha256 = Guard.NotNullOrWhiteSpace(archiveSha256);
        Duration = duration;
    }

    /// <summary>Absolute path of the written archive.</summary>
    public string OutputArchivePath { get; }

    /// <summary>Number of packed files.</summary>
    public int FileCount { get; }

    /// <summary>Total uncompressed bytes packed.</summary>
    public long UncompressedByteCount { get; }

    /// <summary>Lowercase hexadecimal SHA-256 of the finalised archive bytes.</summary>
    public string ArchiveSha256 { get; }

    /// <summary>Wall-clock duration of the pack.</summary>
    public TimeSpan Duration { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutable DTO; §21/§27 integrity hash; §34 SHA-256.
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: None; metadata only (§17).
// - Determinism & Encoding Risks: Hash is hex lowercase, culture-independent.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Hash is computed on the complete finalised archive.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
