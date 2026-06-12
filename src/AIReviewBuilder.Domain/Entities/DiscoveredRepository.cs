using System.Collections.Immutable;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Exceptions;

namespace AIReviewBuilder.Domain.Entities;

/// <summary>
/// An immutable description of a repository discovered on the local machine: its
/// display name and the set of marker <see cref="RepositoryArtifact"/> that caused it
/// to be detected. A repository with no artifacts is not a valid discovery (§25).
/// The absolute root path is intentionally NOT part of the domain entity to keep the
/// domain free of file-system concerns; callers correlate by <see cref="Name"/> and a
/// caller-held path token.
/// </summary>
public sealed class DiscoveredRepository
{
    public DiscoveredRepository(string name, IEnumerable<RepositoryArtifact> artifacts)
    {
        Name = Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(artifacts);

        Artifacts = artifacts.Distinct().OrderBy(a => (int)a).ToImmutableArray();
        if (Artifacts.IsEmpty)
        {
            throw new DomainValidationException(
                $"Repository '{name}' must expose at least one detected artifact.");
        }
    }

    /// <summary>Display name (typically the directory name).</summary>
    public string Name { get; }

    /// <summary>The detected marker artifacts, de-duplicated and deterministically ordered.</summary>
    public ImmutableArray<RepositoryArtifact> Artifacts { get; }

    /// <summary>True when a Git working tree was detected.</summary>
    public bool IsGitRepository => Artifacts.Contains(RepositoryArtifact.GitRepository);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §1/§3 domain purity, §8 deterministic ordering, §25.
// - Architectural & Concurrency Risks: None. Immutable; ImmutableArray field.
// - Security & Trust-Boundary Risks: Domain holds no absolute paths, limiting host
//   path exposure (§17); path handling stays in Infrastructure behind the guard.
// - Determinism & Encoding Risks: Artifacts sorted by ordinal for stable output (§8).
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: At least one artifact is required for a valid repo.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
