using System.Collections.Immutable;
using AIReviewBuilder.Application.Models;

namespace AIReviewBuilder.Application.Abstractions;

/// <summary>
/// Discovers local repositories beneath a search root by detecting marker artifacts
/// (.git, .sln, .csproj, pyproject.toml, package.json). Implemented in Infrastructure.
/// </summary>
public interface IRepositoryDiscoveryService
{
    /// <summary>
    /// Scans <paramref name="searchRootPath"/> (bounded in depth by the implementation)
    /// and returns the repositories found, in deterministic order.
    /// </summary>
    /// <param name="searchRootPath">Absolute directory to scan.</param>
    /// <param name="maxDepth">Maximum directory recursion depth (DoS guard, §10).</param>
    /// <param name="cancellationToken">Cooperative cancellation (§21).</param>
    Task<ImmutableArray<RepositoryLocation>> DiscoverAsync(
        string searchRootPath,
        int maxDepth,
        CancellationToken cancellationToken);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 interface in Application, §10 depth bound, §21 token.
// - Architectural & Concurrency Risks: None. Pure contract.
// - Security & Trust-Boundary Risks: Implementations must apply the path/symlink guard.
// - Determinism & Encoding Risks: Contract requires deterministic ordering.
// - Resource Exhaustion Risks: maxDepth parameter caps recursion.
// - Explicit Assumptions Made: searchRootPath is absolute and accessible.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
