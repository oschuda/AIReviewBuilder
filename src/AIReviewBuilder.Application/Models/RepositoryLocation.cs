using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Entities;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// Pairs a discovered <see cref="DiscoveredRepository"/> (pure domain) with its
/// canonical absolute path on the local machine. The absolute path is an Application
/// concern, deliberately kept out of the domain entity (§3 separation of concerns).
/// </summary>
public sealed class RepositoryLocation
{
    public RepositoryLocation(string absoluteRootPath, DiscoveredRepository repository)
    {
        AbsoluteRootPath = Guard.NotNullOrWhiteSpace(absoluteRootPath);
        Repository = Guard.NotNull(repository);
    }

    /// <summary>Canonical absolute path of the repository root.</summary>
    public string AbsoluteRootPath { get; }

    /// <summary>The discovered repository metadata.</summary>
    public DiscoveredRepository Repository { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 separation, §8 immutable DTO.
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: Holds an absolute path; never logged verbatim
//   (callers log Repository.Name only, per §17).
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: AbsoluteRootPath is already canonicalised by the
//   discovery implementation.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
