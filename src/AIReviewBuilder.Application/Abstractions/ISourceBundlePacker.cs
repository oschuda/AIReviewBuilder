using AIReviewBuilder.Application.Models;

namespace AIReviewBuilder.Application.Abstractions;

/// <summary>
/// Packs clean, filtered source files into a structural <c>.zip</c> archive on the local
/// machine (project spec "Local Repository Packer"). Packing is atomic (temp file,
/// integrity hash, then File.Move). Implemented in Infrastructure.
/// </summary>
public interface ISourceBundlePacker
{
    /// <summary>Packs the files described by <paramref name="request"/> into a zip archive.</summary>
    Task<SourceBundleResult> PackAsync(SourceBundleRequest request, CancellationToken cancellationToken);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 interface in Application; §21 atomic generation.
// - Architectural & Concurrency Risks: None. Pure contract.
// - Security & Trust-Boundary Risks: Implementations must re-validate each entry path
//   against the root and skip symlinks (§9).
// - Determinism & Encoding Risks: Implementations write deterministic entry order and
//   '/'-separated entry names (§8).
// - Resource Exhaustion Risks: Implementations re-enforce ResourceLimits.
// - Explicit Assumptions Made: Output directory is writable.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
