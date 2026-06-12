namespace AIReviewBuilder.Domain.Exceptions;

/// <summary>
/// Structured failure taxonomy required by GLOBAL ENGINEERING STANDARD §11.
/// Generic system exceptions must never be used for domain anomalies; every
/// domain failure is classified by one of these categories.
/// </summary>
public enum FailureCategory
{
    /// <summary>Input or configuration violated a domain invariant.</summary>
    Validation = 1,

    /// <summary>A trust-boundary or security control was violated (e.g. path traversal, symlink escape).</summary>
    Security = 2,

    /// <summary>A configured resource ceiling (file count, size, payload) was exceeded (DoS guard).</summary>
    ResourceExhaustion = 3,

    /// <summary>A technical/infrastructure operation failed (file system, serialization, compression).</summary>
    Infrastructure = 4,
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §11 Error Handling Model & Taxonomy.
// - Architectural & Concurrency Risks: None. Pure enum, immutable by definition.
// - Security & Trust-Boundary Risks: None. Classification only; no logic.
// - Determinism & Encoding Risks: None. Explicit numeric values pin ordinals.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Four categories cover all Phase 1 failure modes.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Additional categories (Integration) may be added
//   when the NET48 bridge layer is introduced in a later phase.
// ─────────────────────────────────────────────────────────────────────────────
