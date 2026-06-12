using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Application.Abstractions;

/// <summary>
/// Scans a repository, applies the Include/Exclude <see cref="FileSelectionCriteria"/>,
/// enforces the <see cref="ResourceLimits"/> DoS ceilings, computes per-file metrics,
/// and returns the selected files. Implemented in Infrastructure.
/// </summary>
public interface IRepositoryFileScanner
{
    /// <summary>
    /// Enumerates files under <paramref name="repositoryRootPath"/>, applying the
    /// criteria and limits. Throws <c>ResourceLimitExceededException</c> on any ceiling
    /// breach and <c>SecurityViolationException</c> on traversal/symlink escape.
    /// </summary>
    Task<FileScanResult> ScanAsync(
        string repositoryRootPath,
        FileSelectionCriteria criteria,
        ResourceLimits limits,
        CancellationToken cancellationToken);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 interface in Application, §10 limits, §21 token.
// - Architectural & Concurrency Risks: None. Pure contract.
// - Security & Trust-Boundary Risks: Contract mandates path/symlink/DoS enforcement
//   in the implementation (fail-fast).
// - Determinism & Encoding Risks: Implementations return deterministic scan order.
// - Resource Exhaustion Risks: Bounded by the supplied ResourceLimits.
// - Explicit Assumptions Made: repositoryRootPath is absolute.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
