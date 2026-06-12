using AIReviewBuilder.Application.Models;

namespace AIReviewBuilder.Application.Abstractions;

/// <summary>
/// Compiles selected, validated, and escaped files into an LLM-optimised <c>review.md</c>.
/// Generation is atomic (temp file, integrity hash, then File.Move). Implemented in
/// Infrastructure (project spec "Markdown Review Generator"; APPLICATION SPEC §21).
/// </summary>
public interface IReviewDocumentGenerator
{
    /// <summary>Generates the review document described by <paramref name="request"/>.</summary>
    Task<ReviewDocumentResult> GenerateAsync(ReviewDocumentRequest request, CancellationToken cancellationToken);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 interface in Application; §21 atomic generation.
// - Architectural & Concurrency Risks: None. Pure contract.
// - Security & Trust-Boundary Risks: Implementations must escape paths/metadata
//   (Markdown injection) and re-validate file paths before reading.
// - Determinism & Encoding Risks: Implementations write UTF-8 without BOM (§8).
// - Resource Exhaustion Risks: Implementations re-enforce ResourceLimits.
// - Explicit Assumptions Made: Output directory is writable.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
