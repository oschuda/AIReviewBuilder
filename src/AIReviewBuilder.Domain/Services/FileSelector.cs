using System.Collections.Immutable;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Domain.Services;

/// <summary>
/// Pure selection logic that decides whether a repository-relative path is included,
/// honouring <see cref="FileSelectionCriteria"/> Include/Exclude sets. Exclude always
/// takes precedence over Include. No I/O — file-system traversal lives in Infrastructure
/// (GLOBAL ENGINEERING STANDARD §1, §3 separation of concerns).
/// </summary>
public static class FileSelector
{
    /// <summary>
    /// Returns true when <paramref name="relativePath"/> matches at least one include
    /// pattern and no exclude pattern.
    /// </summary>
    public static bool IsSelected(string relativePath, FileSelectionCriteria criteria)
    {
        Guard.NotNullOrWhiteSpace(relativePath);
        Guard.NotNull(criteria);

        string normalized = relativePath.Replace('\\', '/').TrimStart('/');

        foreach (GlobPattern exclude in criteria.ExcludePatterns)
        {
            if (exclude.IsMatch(normalized))
            {
                return false;
            }
        }

        foreach (GlobPattern include in criteria.IncludePatterns)
        {
            if (include.IsMatch(normalized))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Filters <paramref name="relativePaths"/> by the criteria, preserving input order
    /// and de-duplicating by ordinal path equality for deterministic output (§8).
    /// </summary>
    public static ImmutableArray<string> Select(IEnumerable<string> relativePaths, FileSelectionCriteria criteria)
    {
        Guard.NotNull(relativePaths);
        Guard.NotNull(criteria);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var builder = ImmutableArray.CreateBuilder<string>();

        foreach (string path in relativePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            string normalized = path.Replace('\\', '/').TrimStart('/');
            if (seen.Add(normalized) && IsSelected(normalized, criteria))
            {
                builder.Add(normalized);
            }
        }

        return builder.ToImmutable();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §1 pure logic, §3 separation, §8 determinism, §14 tests.
// - Architectural & Concurrency Risks: None. Stateless static (§12); local state only.
// - Security & Trust-Boundary Risks: Exclude-wins ordering ensures deny rules cannot
//   be overridden by allow rules; symlink filtering is applied in Infrastructure per
//   criteria.IgnoreSymlinks (requires FileAttributes I/O).
// - Determinism & Encoding Risks: Ordinal de-duplication; input order preserved.
// - Resource Exhaustion Risks: O(paths × patterns); inputs bounded upstream by
//   ResourceLimits.
// - Explicit Assumptions Made: Paths are repository-relative; normalization to '/'.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
