namespace AIReviewBuilder.Domain.Exceptions;

/// <summary>
/// Base type for every domain anomaly. Carries a structured <see cref="FailureCategory"/>
/// so callers and logs can branch on failure type without string matching
/// (GLOBAL ENGINEERING STANDARD §11).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(FailureCategory category, string message)
        : base(message)
    {
        Category = category;
    }

    protected DomainException(FailureCategory category, string message, Exception innerException)
        : base(message, innerException)
    {
        Category = category;
    }

    /// <summary>The structured classification of this failure.</summary>
    public FailureCategory Category { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §11 (typed, structured, traceable exceptions).
// - Architectural & Concurrency Risks: None. Immutable; abstract base prevents
//   accidental use of a generic exception for domain anomalies.
// - Security & Trust-Boundary Risks: Message is caller-supplied; callers must not
//   embed secrets or file contents (§17). Enforced by usage convention + review.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: All concrete domain exceptions derive from this base.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
