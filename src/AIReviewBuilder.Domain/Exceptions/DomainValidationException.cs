namespace AIReviewBuilder.Domain.Exceptions;

/// <summary>
/// Raised when untrusted input violates a domain invariant (§2 explicit validation
/// at all boundaries, §11 typed errors). Fail-fast semantics.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message)
        : base(FailureCategory.Validation, message)
    {
    }

    public DomainValidationException(string message, Exception innerException)
        : base(FailureCategory.Validation, message, innerException)
    {
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §2 / §11.
// - Architectural & Concurrency Risks: None. Sealed, immutable.
// - Security & Trust-Boundary Risks: None directly; conveys validation failures.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Used only for invariant/validation violations.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
