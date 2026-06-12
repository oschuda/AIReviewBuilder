using System.Collections.Immutable;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// Immutable result of scanning and filtering a repository: the selected files (with
/// per-file metrics) and the aggregate <see cref="FileMetrics"/> across all of them.
/// </summary>
public sealed class FileScanResult
{
    public FileScanResult(ImmutableArray<ScannedFile> files, FileMetrics aggregate)
    {
        if (files.IsDefault)
        {
            throw new DomainValidationException($"{nameof(files)} must be initialized.");
        }

        Files = files;
        Aggregate = aggregate;
    }

    /// <summary>The selected files, in deterministic scan order.</summary>
    public ImmutableArray<ScannedFile> Files { get; }

    /// <summary>Aggregate metrics (bytes, lines, approximate tokens) across all files.</summary>
    public FileMetrics Aggregate { get; }

    /// <summary>Number of selected files.</summary>
    public int FileCount => Files.Length;

    public static FileScanResult Empty { get; } =
        new(ImmutableArray<ScannedFile>.Empty, FileMetrics.Empty);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutable DTO.
// - Architectural & Concurrency Risks: None. Immutable; ImmutableArray field.
// - Security & Trust-Boundary Risks: None; carries metadata only (§17).
// - Determinism & Encoding Risks: Files preserve deterministic scan order.
// - Resource Exhaustion Risks: File count is bounded by ResourceLimits before this
//   result is constructed.
// - Explicit Assumptions Made: Aggregate equals the sum of per-file metrics.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
