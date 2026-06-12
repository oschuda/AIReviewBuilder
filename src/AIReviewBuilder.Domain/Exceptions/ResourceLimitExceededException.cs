namespace AIReviewBuilder.Domain.Exceptions;

/// <summary>
/// Raised when a configured Denial-of-Service ceiling is crossed: too many files,
/// a single file larger than the per-file ceiling, or the cumulative payload ceiling
/// (GLOBAL ENGINEERING STANDARD §10 Resource Exhaustion Protection). Fail-fast.
/// </summary>
public sealed class ResourceLimitExceededException : DomainException
{
    public ResourceLimitExceededException(string message)
        : base(FailureCategory.ResourceExhaustion, message)
    {
    }

    /// <summary>The human-readable name of the limit that was exceeded (e.g. "MaxFileCount").</summary>
    public string? LimitName { get; }

    /// <summary>The configured ceiling value.</summary>
    public long? LimitValue { get; }

    /// <summary>The observed value that crossed the ceiling.</summary>
    public long? ObservedValue { get; }

    public ResourceLimitExceededException(string limitName, long limitValue, long observedValue)
        : base(
            FailureCategory.ResourceExhaustion,
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"Resource limit '{limitName}' exceeded: observed {observedValue}, allowed {limitValue}."))
    {
        LimitName = limitName;
        LimitValue = limitValue;
        ObservedValue = observedValue;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §10 DoS ceilings, §11 typed errors.
// - Architectural & Concurrency Risks: None. Sealed, immutable.
// - Security & Trust-Boundary Risks: None; surfaces only numeric metadata, never
//   file contents (§17).
// - Determinism & Encoding Risks: Message built with InvariantCulture (§8).
// - Resource Exhaustion Risks: This type is the signal that a ceiling was hit.
// - Explicit Assumptions Made: Limit/observed values fit in Int64 (bytes & counts).
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
