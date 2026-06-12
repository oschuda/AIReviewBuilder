using AIReviewBuilder.Domain.Services;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Tests.Domain;

public sealed class TokenEstimatorTests
{
    [Fact]
    public void EstimateTokens_EmptyContent_ReturnsZero()
    {
        Assert.Equal(0, TokenEstimator.EstimateTokens(string.Empty));
    }

    [Theory]
    [InlineData("abcd", 1)]      // 4 chars / 4 = 1
    [InlineData("abcdefgh", 2)]  // 8 / 4 = 2
    [InlineData("a", 1)]         // ceil(1/4) = 1
    public void EstimateTokens_UsesCharactersPerToken(string content, int expected)
    {
        Assert.Equal(expected, TokenEstimator.EstimateTokens(content));
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("single line", 1)]
    [InlineData("a\nb", 2)]
    [InlineData("a\r\nb\r\nc", 3)]
    [InlineData("a\rb", 2)]
    [InlineData("trailing\n", 1)]
    public void CountLines_HandlesAllNewlineStyles(string content, int expected)
    {
        Assert.Equal(expected, TokenEstimator.CountLines(content));
    }

    [Fact]
    public void CountLines_IsDeterministicAcrossNewlineEncodings()
    {
        Assert.Equal(
            TokenEstimator.CountLines("a\nb\nc"),
            TokenEstimator.CountLines("a\r\nb\r\nc"));
    }

    [Fact]
    public void EstimateMetrics_PopulatesAllFields()
    {
        FileMetrics metrics = TokenEstimator.EstimateMetrics("abcd\nefgh", byteCount: 9);

        Assert.Equal(9, metrics.ByteCount);
        Assert.Equal(2, metrics.LineCount);
        Assert.True(metrics.ApproximateTokenCount > 0);
    }
}
