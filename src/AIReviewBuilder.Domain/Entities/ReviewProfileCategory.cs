namespace AIReviewBuilder.Domain.Entities;

/// <summary>
/// The built-in review profile categories shipped with Phase 1
/// (project spec: General, Security, Architecture, Performance, Documentation).
/// </summary>
public enum ReviewProfileCategory
{
    General = 0,
    Security = 1,
    Architecture = 2,
    Performance = 3,
    Documentation = 4,
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — domain model, no external dependencies (§1/§3).
// - Architectural & Concurrency Risks: None. Pure enum.
// - Security & Trust-Boundary Risks: None.
// - Determinism & Encoding Risks: Explicit ordinals pinned for deterministic
//   (de)serialization (§8).
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: The five categories match the project specification.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
