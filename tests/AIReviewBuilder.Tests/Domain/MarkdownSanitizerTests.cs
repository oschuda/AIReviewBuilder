using AIReviewBuilder.Domain.Services;

namespace AIReviewBuilder.Tests.Domain;

public sealed class MarkdownSanitizerTests
{
    [Theory]
    [InlineData("plain.cs", "plain.cs")]
    [InlineData("a|b", "a\\|b")]
    [InlineData("# heading", "\\# heading")]
    [InlineData("a*b_c", "a\\*b\\_c")]
    [InlineData("<script>", "\\<script\\>")]
    [InlineData("back`tick", "back\\`tick")]
    public void EscapeInline_EscapesControlMarkdown(string input, string expected)
    {
        Assert.Equal(expected, MarkdownSanitizer.EscapeInline(input));
    }

    [Fact]
    public void EscapeInline_FlattensLineBreaks()
    {
        Assert.Equal("a b c", MarkdownSanitizer.EscapeInline("a\nb\tc"));
    }

    [Fact]
    public void EscapeInline_DropsControlCharacters()
    {
        Assert.Equal("ab", MarkdownSanitizer.EscapeInline("a\u0000b"));
    }

    [Fact]
    public void SelectCodeFence_NoBackticks_ReturnsThreeBackticks()
    {
        Assert.Equal("```", MarkdownSanitizer.SelectCodeFence("no fences here"));
    }

    [Fact]
    public void SelectCodeFence_LongerThanLongestInternalRun()
    {
        string content = "code with ```` four backticks";
        string fence = MarkdownSanitizer.SelectCodeFence(content);
        Assert.Equal(5, fence.Length);
        Assert.DoesNotContain(fence, content);
    }
}
