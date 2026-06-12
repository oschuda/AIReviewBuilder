using System.Runtime.CompilerServices;
using AIReviewBuilder.Domain.Exceptions;

namespace AIReviewBuilder.Domain.Common;

/// <summary>
/// Centralised argument and invariant guards. Every public domain boundary validates
/// its inputs through these helpers (GLOBAL ENGINEERING STANDARD §2 explicit
/// validation at all boundaries). Violations throw a typed <see cref="DomainValidationException"/>.
/// </summary>
public static class Guard
{
    /// <summary>Ensures a reference argument is not null.</summary>
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        if (value is null)
        {
            throw new DomainValidationException($"Argument '{paramName}' must not be null.");
        }

        return value;
    }

    /// <summary>Ensures a string is neither null nor whitespace.</summary>
    public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"Argument '{paramName}' must not be null or whitespace.");
        }

        return value;
    }

    /// <summary>Ensures a numeric argument is strictly positive (&gt; 0).</summary>
    public static long Positive(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
        {
            throw new DomainValidationException($"Argument '{paramName}' must be positive but was {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}.");
        }

        return value;
    }

    /// <summary>Ensures a numeric argument is zero or positive (&gt;= 0).</summary>
    public static long NotNegative(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0)
        {
            throw new DomainValidationException($"Argument '{paramName}' must not be negative but was {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}.");
        }

        return value;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §2 boundary validation, §11 typed failures.
// - Architectural & Concurrency Risks: None. Stateless static helpers (§12).
// - Security & Trust-Boundary Risks: None; this is the validation primitive others
//   build on. Does not log or expose input values beyond the thrown message.
// - Determinism & Encoding Risks: Numeric formatting uses InvariantCulture (§8).
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: CallerArgumentExpression yields meaningful names.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
