using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Exceptions;

namespace AIReviewBuilder.Domain.Entities;

/// <summary>
/// An immutable review profile: a named, categorised LLM prompt used to steer a review
/// (project spec "Review Profiles"). Invariants are enforced at construction so an
/// invalid profile cannot exist (GLOBAL ENGINEERING STANDARD §25 domain integrity).
/// </summary>
public sealed class ReviewProfile
{
    /// <summary>Maximum length for short string fields (APPLICATION SPEC §5: 255 chars).</summary>
    public const int MaxShortStringLength = 255;

    /// <summary>Maximum length for the prompt body (defensive bound against unbounded input).</summary>
    public const int MaxPromptLength = 32_768;

    public ReviewProfile(string id, string name, ReviewProfileCategory category, string prompt, string? description = null)
    {
        Id = ValidateShort(id, nameof(id));
        Name = ValidateShort(name, nameof(name));

        if (!Enum.IsDefined(category))
        {
            throw new DomainValidationException($"Unknown {nameof(ReviewProfileCategory)} value '{(int)category}'.");
        }

        Category = category;

        Guard.NotNullOrWhiteSpace(prompt);
        if (prompt.Length > MaxPromptLength)
        {
            throw new DomainValidationException(
                $"Profile prompt exceeds the maximum length of {MaxPromptLength} characters.");
        }

        Prompt = prompt;

        if (description is not null && description.Length > MaxShortStringLength)
        {
            throw new DomainValidationException(
                $"Profile description exceeds the maximum length of {MaxShortStringLength} characters.");
        }

        Description = description;
    }

    /// <summary>Stable identifier (e.g. "security").</summary>
    public string Id { get; }

    /// <summary>Human-readable name.</summary>
    public string Name { get; }

    /// <summary>The category this profile belongs to.</summary>
    public ReviewProfileCategory Category { get; }

    /// <summary>The prompt body injected into the generated review.</summary>
    public string Prompt { get; }

    /// <summary>Optional short description.</summary>
    public string? Description { get; }

    private static string ValidateShort(string value, string paramName)
    {
        Guard.NotNullOrWhiteSpace(value, paramName);
        if (value.Length > MaxShortStringLength)
        {
            throw new DomainValidationException(
                $"'{paramName}' exceeds the maximum length of {MaxShortStringLength} characters.");
        }

        return value;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §25 invariants by construction; §5 string-length
//   bounds; §2 validation.
// - Architectural & Concurrency Risks: None. Immutable; safe to share.
// - Security & Trust-Boundary Risks: Bounded prompt/name lengths mitigate
//   long-string/injection abuse; content escaping for Markdown happens at render
//   time (MarkdownSanitizer), not here.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: Prompt length capped (§10).
// - Explicit Assumptions Made: Enum.IsDefined is acceptable for this small, stable
//   non-flags enum.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
