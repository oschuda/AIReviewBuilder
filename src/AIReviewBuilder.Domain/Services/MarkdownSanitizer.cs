using System.Text;
using AIReviewBuilder.Domain.Common;

namespace AIReviewBuilder.Domain.Services;

/// <summary>
/// Markdown-injection mitigation (project spec; GLOBAL ENGINEERING STANDARD §2 input
/// treated as untrusted). File paths and metadata headers are escaped before being
/// written into the generated <c>review.md</c> so that hostile names cannot break the
/// document layout, forge headings/tables, or inject raw HTML. Pure, deterministic.
/// </summary>
public static class MarkdownSanitizer
{
    /// <summary>
    /// Escapes a value for safe use as inline Markdown text (paths, metadata values).
    /// Line breaks are flattened to spaces and Markdown/HTML control characters are
    /// backslash-escaped so the value renders literally.
    /// </summary>
    public static string EscapeInline(string value)
    {
        Guard.NotNull(value);
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length + 8);
        foreach (char c in value)
        {
            switch (c)
            {
                case '\r':
                case '\n':
                case '\t':
                    sb.Append(' ');
                    break;

                case '\\':
                case '`':
                case '*':
                case '_':
                case '{':
                case '}':
                case '[':
                case ']':
                case '(':
                case ')':
                case '#':
                case '+':
                case '-':
                case '!':
                case '|':
                case '<':
                case '>':
                case '~':
                    sb.Append('\\').Append(c);
                    break;

                default:
                    // Drop other C0 control characters that could corrupt layout.
                    if (!char.IsControl(c))
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a backtick fence (length &gt;= 3) guaranteed to be longer than the longest
    /// run of backticks inside <paramref name="content"/>, so the content cannot escape
    /// the code block it will be wrapped in (CommonMark fenced-code rule).
    /// </summary>
    public static string SelectCodeFence(string content)
    {
        Guard.NotNull(content);

        int longestRun = 0;
        int currentRun = 0;
        foreach (char c in content)
        {
            if (c == '`')
            {
                currentRun++;
                if (currentRun > longestRun)
                {
                    longestRun = currentRun;
                }
            }
            else
            {
                currentRun = 0;
            }
        }

        int fenceLength = Math.Max(3, longestRun + 1);
        return new string('`', fenceLength);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — project Markdown-injection requirement; §2 untrusted
//   input; §8 determinism.
// - Architectural & Concurrency Risks: None. Stateless static (§12).
// - Security & Trust-Boundary Risks: EscapeInline neutralises headings, emphasis,
//   tables, links/HTML, and control chars; SelectCodeFence prevents fenced-code
//   breakout for embedded file contents. Together they close the documented injection
//   vectors for review.md.
// - Determinism & Encoding Risks: Pure char-by-char transform; no culture use.
// - Resource Exhaustion Risks: O(n) single pass; inputs bounded upstream.
// - Explicit Assumptions Made: Output is consumed by a CommonMark-compatible renderer;
//   fenced-code escaping follows the longest-backtick-run rule.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Renderers with non-standard extensions could
//   introduce new vectors; the escaper targets CommonMark + GFM tables/HTML.
// ─────────────────────────────────────────────────────────────────────────────
