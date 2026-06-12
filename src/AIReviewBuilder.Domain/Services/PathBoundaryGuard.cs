using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Exceptions;

namespace AIReviewBuilder.Domain.Services;

/// <summary>
/// Path-traversal guard (GLOBAL ENGINEERING STANDARD §9 trust boundaries; OWASP §5).
/// Every candidate path is resolved with <see cref="Path.GetFullPath(string)"/> and
/// then structurally verified to remain strictly within a designated repository root.
/// Any escape (../../, absolute path injection, drive change) fails closed with a
/// <see cref="SecurityViolationException"/> (§19 fail-closed).
///
/// This type performs only in-memory path arithmetic (no file-system access), so it
/// remains pure domain logic and is exhaustively unit-testable (§14).
/// </summary>
public static class PathBoundaryGuard
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Resolves <paramref name="relativeOrAbsolutePath"/> against <paramref name="repositoryRoot"/>
    /// and guarantees the result is contained within the root. Returns the canonical
    /// absolute path. Throws <see cref="SecurityViolationException"/> on any escape.
    /// </summary>
    public static string ResolveWithinRoot(string repositoryRoot, string relativeOrAbsolutePath)
    {
        Guard.NotNullOrWhiteSpace(repositoryRoot);
        Guard.NotNullOrWhiteSpace(relativeOrAbsolutePath);

        string resolvedRoot = NormalizeRoot(repositoryRoot);

        // GetFullPath(path, basePath) collapses '..'/'.'. If the candidate is itself
        // absolute it is returned unchanged — the containment check below still rejects it.
        string resolvedCandidate = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(relativeOrAbsolutePath, resolvedRoot));

        if (!IsContained(resolvedRoot, resolvedCandidate))
        {
            throw new SecurityViolationException(
                $"Path '{relativeOrAbsolutePath}' resolves outside the repository root boundary.");
        }

        return resolvedCandidate;
    }

    /// <summary>
    /// Validates that an already-absolute <paramref name="absoluteCandidatePath"/> lies
    /// within <paramref name="repositoryRoot"/>. Returns the canonical path or throws.
    /// </summary>
    public static string EnsureWithinRoot(string repositoryRoot, string absoluteCandidatePath)
    {
        Guard.NotNullOrWhiteSpace(repositoryRoot);
        Guard.NotNullOrWhiteSpace(absoluteCandidatePath);

        string resolvedRoot = NormalizeRoot(repositoryRoot);
        string resolvedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(absoluteCandidatePath));

        if (!IsContained(resolvedRoot, resolvedCandidate))
        {
            throw new SecurityViolationException(
                $"Path '{absoluteCandidatePath}' is outside the repository root boundary.");
        }

        return resolvedCandidate;
    }

    /// <summary>
    /// Non-throwing containment test against already-canonicalised paths. Returns true
    /// when <paramref name="resolvedCandidate"/> equals the root or is nested beneath it.
    /// </summary>
    public static bool IsContained(string resolvedRoot, string resolvedCandidate)
    {
        Guard.NotNullOrWhiteSpace(resolvedRoot);
        Guard.NotNullOrWhiteSpace(resolvedCandidate);

        string root = Path.TrimEndingDirectorySeparator(resolvedRoot);
        string candidate = Path.TrimEndingDirectorySeparator(resolvedCandidate);

        if (string.Equals(root, candidate, PathComparison))
        {
            return true;
        }

        string rootWithSeparator = root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSeparator, PathComparison);
    }

    private static string NormalizeRoot(string repositoryRoot)
    {
        string resolved = Path.GetFullPath(repositoryRoot);
        if (!Path.IsPathRooted(resolved))
        {
            throw new SecurityViolationException("Repository root must resolve to an absolute path.");
        }

        return Path.TrimEndingDirectorySeparator(resolved);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §9 trust boundaries, §19 fail-closed, §14 testability.
// - Architectural & Concurrency Risks: None. Stateless static (§12).
// - Security & Trust-Boundary Risks: Mitigates ../../ traversal AND absolute-path
//   injection (an absolute candidate survives GetFullPath but is rejected by the
//   containment check). Uses a separator-anchored prefix test to avoid the
//   "/root" vs "/rootsibling" false-positive. Platform-correct case sensitivity.
// - Determinism & Encoding Risks: Comparison rule is selected by OS once; pure string
//   arithmetic otherwise.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Symlink resolution is an Infrastructure responsibility
//   (requires I/O); this guard handles textual traversal only. Callers combine both.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: On Windows, short (8.3) names or alternate data
//   streams are not canonicalised here; Infrastructure must reject reparse points and
//   may additionally canonicalise via the file system before trusting a path.
// ─────────────────────────────────────────────────────────────────────────────
