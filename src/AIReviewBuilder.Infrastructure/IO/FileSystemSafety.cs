using AIReviewBuilder.Domain.Common;

namespace AIReviewBuilder.Infrastructure.IO;

/// <summary>
/// Small, side-effect-light helpers for safe file-system inspection. Symlink/junction
/// detection backs the "Symlink Defense" requirement (GLOBAL ENGINEERING STANDARD §9):
/// reparse points are detected so callers can skip them and avoid leaking content from
/// outside the repository boundary.
/// </summary>
public static class FileSystemSafety
{
    /// <summary>
    /// Returns true when the entry is a symbolic link, junction, or other reparse point.
    /// Determined from <see cref="FileAttributes.ReparsePoint"/>.
    /// </summary>
    public static bool IsReparsePoint(FileSystemInfo entry)
    {
        Guard.NotNull(entry);
        return entry.Exists && entry.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §9 symlink defense primitive.
// - Architectural & Concurrency Risks: None. Stateless static (§12).
// - Security & Trust-Boundary Risks: Central, testable reparse-point check used to
//   exclude symlinks/junctions from traversal and packing.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: FileAttributes is populated by the caller's
//   DirectoryInfo/FileInfo enumeration; the OS reports reparse points via attributes.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Exotic mount types not surfaced as ReparsePoint are out
//   of scope; combined with PathBoundaryGuard containment this is defense-in-depth.
// ─────────────────────────────────────────────────────────────────────────────
