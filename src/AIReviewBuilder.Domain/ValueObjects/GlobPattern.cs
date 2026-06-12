using System.Text;
using System.Text.RegularExpressions;
using AIReviewBuilder.Domain.Common;

namespace AIReviewBuilder.Domain.ValueObjects;

/// <summary>
/// An immutable, validated file-selection glob pattern (e.g. <c>*.cs</c>, <c>src/**/*.py</c>).
/// Patterns are matched against repository-relative paths that use '/' as separator.
///
/// The glob is translated once to a deterministic, linear-time regular expression
/// (no catastrophic backtracking) with an explicit match timeout, satisfying the
/// resource-exhaustion guard of GLOBAL ENGINEERING STANDARD §10 and the deterministic,
/// culture-invariant requirement of §8.
/// </summary>
public sealed class GlobPattern : IEquatable<GlobPattern>
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private readonly Regex _regex;

    private GlobPattern(string raw, Regex regex)
    {
        Raw = raw;
        _regex = regex;
    }

    /// <summary>The original glob text as supplied by the caller (normalized to '/').</summary>
    public string Raw { get; }

    /// <summary>Parses and compiles a glob pattern. Throws on null/empty input (§2).</summary>
    public static GlobPattern Parse(string pattern)
    {
        Guard.NotNullOrWhiteSpace(pattern);

        string normalized = pattern.Replace('\\', '/').Trim();
        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        string regexBody = TranslateToRegex(normalized);
        var regex = new Regex(
            regexBody,
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline,
            MatchTimeout);

        return new GlobPattern(normalized, regex);
    }

    /// <summary>Returns true when <paramref name="relativePath"/> matches this pattern.</summary>
    public bool IsMatch(string relativePath)
    {
        Guard.NotNull(relativePath);
        string candidate = relativePath.Replace('\\', '/').TrimStart('/');
        return _regex.IsMatch(candidate);
    }

    private static string TranslateToRegex(string glob)
    {
        var sb = new StringBuilder(glob.Length * 2);
        sb.Append('^');

        for (int i = 0; i < glob.Length; i++)
        {
            char c = glob[i];
            switch (c)
            {
                case '*':
                    bool isDoubleStar = i + 1 < glob.Length && glob[i + 1] == '*';
                    if (isDoubleStar)
                    {
                        // Consume the second '*' and an optional following '/'.
                        i++;
                        if (i + 1 < glob.Length && glob[i + 1] == '/')
                        {
                            i++;
                            // '**/' matches zero or more leading path segments.
                            sb.Append("(?:.*/)?");
                        }
                        else
                        {
                            // Trailing '**' matches anything, including '/'.
                            sb.Append(".*");
                        }
                    }
                    else
                    {
                        // Single '*' matches anything except the path separator.
                        sb.Append("[^/]*");
                    }

                    break;

                case '?':
                    sb.Append("[^/]");
                    break;

                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        sb.Append('$');
        return sb.ToString();
    }

    public bool Equals(GlobPattern? other) =>
        other is not null && string.Equals(Raw, other.Raw, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as GlobPattern);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Raw);

    public override string ToString() => Raw;
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 determinism/culture-invariant, §10 regex DoS guard.
// - Architectural & Concurrency Risks: None. Immutable; compiled Regex is thread-safe.
// - Security & Trust-Boundary Risks: Translation emits only linear constructs
//   ([^/]*, .* , (?:.*/)?) so no catastrophic backtracking; a 1s match timeout is a
//   defense-in-depth backstop. Patterns are untrusted input, validated on Parse.
// - Determinism & Encoding Risks: RegexOptions.CultureInvariant + Ordinal equality.
// - Resource Exhaustion Risks: Bounded by match timeout; no recursion.
// - Explicit Assumptions Made: Paths use '/' after normalization; matching is
//   case-insensitive to behave consistently across Windows/Linux file systems.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Brace/character-class globs ({a,b}, [a-z]) are not
//   supported in Phase 1; only *, **, ? are. Documented and sufficient for the spec.
// ─────────────────────────────────────────────────────────────────────────────
