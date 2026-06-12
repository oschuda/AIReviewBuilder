using AIReviewBuilder.Application.Models;

namespace AIReviewBuilder.Application.Abstractions;

/// <summary>
/// Coordinates an end-to-end review build (scan → optional review.md → optional .zip).
/// This is the single composition point for the Phase 1 workflow (§3, §10 extension by
/// composition).
/// </summary>
public interface IReviewOrchestrator
{
    /// <summary>Executes the build described by <paramref name="request"/>.</summary>
    Task<ReviewBuildResult> BuildAsync(ReviewBuildRequest request, CancellationToken cancellationToken);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 orchestration contract in Application.
// - Architectural & Concurrency Risks: None. Pure contract.
// - Security & Trust-Boundary Risks: None; delegates to guarded implementations.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: A single build runs per call.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
