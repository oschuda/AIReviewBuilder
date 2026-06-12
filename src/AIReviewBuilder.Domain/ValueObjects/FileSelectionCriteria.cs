using System.Collections.Immutable;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Exceptions;

namespace AIReviewBuilder.Domain.ValueObjects;

/// <summary>
/// Immutable description of which repository-relative paths should be selected:
/// an explicit Include set and an explicit Exclude set of <see cref="GlobPattern"/>,
/// plus the symlink-handling policy. Exclude always wins over Include.
/// (GLOBAL ENGINEERING STANDARD §8 immutable DTOs; project spec "Include and Exclude structures".)
/// </summary>
public sealed class FileSelectionCriteria
{
    public FileSelectionCriteria(
        IEnumerable<GlobPattern> includePatterns,
        IEnumerable<GlobPattern> excludePatterns,
        bool ignoreSymlinks = true)
    {
        Guard.NotNull(includePatterns);
        Guard.NotNull(excludePatterns);

        IncludePatterns = includePatterns.ToImmutableArray();
        ExcludePatterns = excludePatterns.ToImmutableArray();

        if (IncludePatterns.IsEmpty)
        {
            throw new DomainValidationException("At least one include pattern is required.");
        }

        IgnoreSymlinks = ignoreSymlinks;
    }

    /// <summary>Patterns a path must match at least one of to be eligible.</summary>
    public ImmutableArray<GlobPattern> IncludePatterns { get; }

    /// <summary>Patterns that, if matched, unconditionally exclude a path.</summary>
    public ImmutableArray<GlobPattern> ExcludePatterns { get; }

    /// <summary>When true, symbolic links are excluded to prevent host directory leakage (§9).</summary>
    public bool IgnoreSymlinks { get; }

    /// <summary>
    /// The default Phase 1 source patterns (*.cs, *.py, *.md, *.json, *.yaml) with a
    /// conservative exclude list for build output and VCS metadata.
    /// </summary>
    public static FileSelectionCriteria Default { get; } = new(
        includePatterns:
        [
            GlobPattern.Parse("**/*.cs"),
            GlobPattern.Parse("**/*.py"),
            GlobPattern.Parse("**/*.md"),
            GlobPattern.Parse("**/*.json"),
            GlobPattern.Parse("**/*.yaml"),
            GlobPattern.Parse("**/*.yml"),
        ],
        excludePatterns:
        [
            GlobPattern.Parse("**/.git/**"),
            GlobPattern.Parse("**/bin/**"),
            GlobPattern.Parse("**/obj/**"),
            GlobPattern.Parse("**/node_modules/**"),
            GlobPattern.Parse("**/.venv/**"),
            GlobPattern.Parse("**/__pycache__/**"),
        ],
        ignoreSymlinks: true);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutability, §2 validation, §9 symlink policy.
// - Architectural & Concurrency Risks: None. ImmutableArray fields; safe to share.
// - Security & Trust-Boundary Risks: IgnoreSymlinks defaults to true (fail-safe);
//   default excludes keep VCS/build artifacts out of generated context.
// - Determinism & Encoding Risks: Pattern order is preserved deterministically.
// - Resource Exhaustion Risks: None directly; pattern count is caller-controlled.
// - Explicit Assumptions Made: Empty include set is invalid (would select nothing
//   useful or, if inverted, everything — both are footguns), so it fails fast.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
