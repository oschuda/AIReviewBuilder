using AIReviewBuilder.Domain.Common;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// Immutable aggregate outcome of an end-to-end review build, tagged with the
/// correlation id used across all log entries (§17 traceability).
/// </summary>
public sealed class ReviewBuildResult
{
    public ReviewBuildResult(
        string correlationId,
        FileScanResult scan,
        ReviewDocumentResult? document,
        SourceBundleResult? bundle,
        TimeSpan totalDuration)
    {
        CorrelationId = Guard.NotNullOrWhiteSpace(correlationId);
        Scan = Guard.NotNull(scan);
        Document = document;
        Bundle = bundle;
        TotalDuration = totalDuration;
    }

    /// <summary>Correlation id chaining every log entry for this build.</summary>
    public string CorrelationId { get; }

    /// <summary>The scan/selection result.</summary>
    public FileScanResult Scan { get; }

    /// <summary>The generated review document, if requested.</summary>
    public ReviewDocumentResult? Document { get; }

    /// <summary>The generated source bundle, if requested.</summary>
    public SourceBundleResult? Bundle { get; }

    /// <summary>Total wall-clock duration of the build.</summary>
    public TimeSpan TotalDuration { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutable DTO; §17 correlation id.
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: None; metadata only (§17).
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Document/Bundle are null when not requested.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
