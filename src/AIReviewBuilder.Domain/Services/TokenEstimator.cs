using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Domain.Services;

/// <summary>
/// Deterministic, culture-invariant estimator for line counts and approximate LLM
/// token counts (project spec "Token &amp; Metric Estimation"). The estimate is a pure
/// function of its input — no I/O, no locale, no clock — satisfying GLOBAL ENGINEERING
/// STANDARD §8 (determinism) and §1 (pure domain logic).
/// </summary>
public static class TokenEstimator
{
    /// <summary>
    /// Average characters per token. 4.0 is the widely used rule-of-thumb for English
    /// source text with common BPE tokenizers; it is a heuristic, not exact.
    /// </summary>
    public const double CharactersPerToken = 4.0;

    /// <summary>
    /// Estimates the number of tokens in <paramref name="content"/> as
    /// ceil(charCount / <see cref="CharactersPerToken"/>). Deterministic across runs.
    /// </summary>
    public static int EstimateTokens(string content)
    {
        Guard.NotNull(content);
        if (content.Length == 0)
        {
            return 0;
        }

        double estimate = content.Length / CharactersPerToken;
        return (int)Math.Ceiling(estimate);
    }

    /// <summary>
    /// Counts logical lines: the number of line terminators plus one for a final
    /// non-terminated line. An empty string has zero lines. CR, LF, and CRLF are all
    /// treated as a single terminator deterministically.
    /// </summary>
    public static int CountLines(string content)
    {
        Guard.NotNull(content);
        if (content.Length == 0)
        {
            return 0;
        }

        int lines = 0;
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (c == '\n')
            {
                lines++;
            }
            else if (c == '\r')
            {
                lines++;
                // Treat CRLF as one terminator.
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }
            }
        }

        // Count a trailing line that is not newline-terminated.
        char last = content[^1];
        if (last is not ('\n' or '\r'))
        {
            lines++;
        }

        return lines;
    }

    /// <summary>
    /// Computes the full <see cref="FileMetrics"/> for decoded text content given the
    /// authoritative on-disk <paramref name="byteCount"/> (supplied by Infrastructure).
    /// </summary>
    public static FileMetrics EstimateMetrics(string content, long byteCount)
    {
        Guard.NotNull(content);
        Guard.NotNegative(byteCount);
        return new FileMetrics(byteCount, CountLines(content), EstimateTokens(content));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §1 pure domain logic, §8 determinism, §14 testability.
// - Architectural & Concurrency Risks: None. Stateless static (§12), thread-safe.
// - Security & Trust-Boundary Risks: None; operates on already-decoded, bounded text.
// - Determinism & Encoding Risks: No culture/locale use; CR/LF/CRLF handled
//   explicitly; token math is integer-ceiling and reproducible.
// - Resource Exhaustion Risks: O(n) single pass; input size is bounded upstream by
//   ResourceLimits before content reaches this estimator.
// - Explicit Assumptions Made: 4 chars/token heuristic is acceptable for the
//   "approximate" metric required by the spec; byteCount is provided by the caller.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Exact tokenization differs per LLM; only an
//   approximation is promised by the specification.
// ─────────────────────────────────────────────────────────────────────────────
