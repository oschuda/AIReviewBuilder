namespace AIReviewBuilder.Domain.Entities;

/// <summary>
/// A marker artifact that identifies a directory as a source repository
/// (project spec: .git, .sln, .csproj, pyproject.toml, package.json).
/// </summary>
public enum RepositoryArtifact
{
    /// <summary>A <c>.git</c> directory (Git working tree).</summary>
    GitRepository = 0,

    /// <summary>A Visual Studio <c>.sln</c> solution file.</summary>
    VisualStudioSolution = 1,

    /// <summary>A C# <c>.csproj</c> project file.</summary>
    CSharpProject = 2,

    /// <summary>A Python <c>pyproject.toml</c> file.</summary>
    PythonProject = 3,

    /// <summary>A Node.js <c>package.json</c> file.</summary>
    NodeProject = 4,
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — domain model (§1/§3); matches project spec markers.
// - Architectural & Concurrency Risks: None. Pure enum.
// - Security & Trust-Boundary Risks: None.
// - Determinism & Encoding Risks: Explicit ordinals pinned (§8).
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Detection of the artifact files is an Infrastructure
//   concern; this enum only names the artifact kinds.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
