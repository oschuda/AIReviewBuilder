namespace AIReviewBuilder.Domain.Exceptions;

/// <summary>
/// Raised when a security control is violated: path traversal outside the repository
/// root, symbolic-link escape, or any attempt to cross a trust boundary
/// (GLOBAL ENGINEERING STANDARD §9, OWASP §5). Always fail-closed (§19).
/// </summary>
public sealed class SecurityViolationException : DomainException
{
    public SecurityViolationException(string message)
        : base(FailureCategory.Security, message)
    {
    }

    public SecurityViolationException(string message, Exception innerException)
        : base(FailureCategory.Security, message, innerException)
    {
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §9 Trust Boundaries, §19 fail-closed.
// - Architectural & Concurrency Risks: None. Sealed, immutable.
// - Security & Trust-Boundary Risks: Carries security failures explicitly so they
//   cannot be confused with ordinary validation errors.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Messages describe the violation without leaking
//   absolute host paths beyond what the caller deems safe (§17).
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
