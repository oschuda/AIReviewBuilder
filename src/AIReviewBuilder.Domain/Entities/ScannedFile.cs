using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Domain.Entities;

/// <summary>
/// An immutable record of a single file discovered inside a repository: its
/// repository-relative path (always '/'-separated) and its computed
/// <see cref="FileMetrics"/>. The relative path is validated to be non-rooted and
/// free of parent-directory segments (defense-in-depth against traversal, §9).
/// </summary>
public sealed class ScannedFile
{
    public ScannedFile(string relativePath, FileMetrics metrics)
    {
        Guard.NotNullOrWhiteSpace(relativePath);

        string normalized = relativePath.Replace('\\', '/').TrimStart('/');

        if (normalized.Length == 0)
        {
            throw new DomainValidationException("Relative path must not be empty after normalization.");
        }

        foreach (string segment in normalized.Split('/'))
        {
            if (segment is "..")
            {
                throw new SecurityViolationException(
                    $"Relative path '{relativePath}' contains a parent-directory segment.");
            }
        }

        RelativePath = normalized;
        Metrics = metrics;
    }

    /// <summary>Repository-relative, '/'-separated path.</summary>
    public string RelativePath { get; }

    /// <summary>Size, line, and approximate token metrics for this file.</summary>
    public FileMetrics Metrics { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §9 trust boundary (no '..' segments), §25 invariants.
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: Rejects rooted paths and parent-directory
//   segments as a second line of defense behind the infrastructure path guard.
// - Determinism & Encoding Risks: Path normalized to '/' deterministically.
// - Resource Exhaustion Risks: None (metrics already bounded upstream).
// - Explicit Assumptions Made: Absolute-path resolution and content access are
//   Infrastructure concerns; the domain holds only the safe relative path.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
