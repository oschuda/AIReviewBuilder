namespace AIReviewBuilder.Domain.Exceptions;

/// <summary>
/// Raised when a technical/infrastructure operation fails (file system access,
/// serialization, compression) in a way that is not a validation or security anomaly
/// (GLOBAL ENGINEERING STANDARD §11 structured taxonomy). Wraps the underlying
/// technical exception so no failure is silently swallowed.
/// </summary>
public sealed class InfrastructureException : DomainException
{
    public InfrastructureException(string message)
        : base(FailureCategory.Infrastructure, message)
    {
    }

    public InfrastructureException(string message, Exception innerException)
        : base(FailureCategory.Infrastructure, message, innerException)
    {
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §11 typed errors; §11 "no swallowed exceptions"
//   (preserves inner exception).
// - Architectural & Concurrency Risks: None. Sealed, immutable.
// - Security & Trust-Boundary Risks: Messages must avoid leaking file contents or
//   secrets (§17); callers pass metadata-only messages.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Used only for genuine technical failures, not domain
//   anomalies.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
