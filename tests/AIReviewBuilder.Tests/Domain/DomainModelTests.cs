using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Tests.Domain;

public sealed class DomainModelTests
{
    [Fact]
    public void ResourceLimits_Default_HasSpecMandatedCeilings()
    {
        ResourceLimits limits = ResourceLimits.Default;
        Assert.Equal(5_000, limits.MaxFileCount);
        Assert.Equal(5L * 1024 * 1024, limits.MaxSingleFileBytes);
        Assert.Equal(100L * 1024 * 1024, limits.MaxCumulativeBytes);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void ResourceLimits_NonPositiveBounds_Throw(int files, long single, long cumulative)
    {
        Assert.Throws<DomainValidationException>(
            () => new ResourceLimits(files, single, cumulative));
    }

    [Fact]
    public void ResourceLimits_EnforcementThrowsOnBreach()
    {
        var limits = new ResourceLimits(2, 10, 20);

        Assert.Throws<ResourceLimitExceededException>(() => limits.EnsureFileCountWithinLimit(3));
        Assert.Throws<ResourceLimitExceededException>(() => limits.EnsureSingleFileWithinLimit(11));
        Assert.Throws<ResourceLimitExceededException>(() => limits.EnsureCumulativeWithinLimit(21));

        // Boundary values are allowed.
        limits.EnsureFileCountWithinLimit(2);
        limits.EnsureSingleFileWithinLimit(10);
        limits.EnsureCumulativeWithinLimit(20);
    }

    [Fact]
    public void FileMetrics_Add_SumsComponents()
    {
        var a = new FileMetrics(10, 2, 3);
        var b = new FileMetrics(5, 1, 1);
        FileMetrics sum = a.Add(b);

        Assert.Equal(15, sum.ByteCount);
        Assert.Equal(3, sum.LineCount);
        Assert.Equal(4, sum.ApproximateTokenCount);
    }

    [Fact]
    public void ScannedFile_NormalizesSeparatorsAndStripsLeadingSlash()
    {
        var file = new ScannedFile("/src\\app/Program.cs", FileMetrics.Empty);
        Assert.Equal("src/app/Program.cs", file.RelativePath);
    }

    [Fact]
    public void ScannedFile_ParentSegment_Throws()
    {
        Assert.Throws<SecurityViolationException>(
            () => new ScannedFile("src/../escape.cs", FileMetrics.Empty));
    }

    [Fact]
    public void ReviewProfile_RejectsOverlongPrompt()
    {
        string prompt = new('x', ReviewProfile.MaxPromptLength + 1);
        Assert.Throws<DomainValidationException>(
            () => new ReviewProfile("id", "name", ReviewProfileCategory.General, prompt));
    }

    [Fact]
    public void ReviewProfile_RejectsUndefinedCategory()
    {
        Assert.Throws<DomainValidationException>(
            () => new ReviewProfile("id", "name", (ReviewProfileCategory)999, "prompt"));
    }

    [Fact]
    public void DiscoveredRepository_NoArtifacts_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => new DiscoveredRepository("repo", []));
    }

    [Fact]
    public void DiscoveredRepository_DeduplicatesAndOrdersArtifacts()
    {
        var repo = new DiscoveredRepository(
            "repo",
            [RepositoryArtifact.CSharpProject, RepositoryArtifact.GitRepository, RepositoryArtifact.CSharpProject]);

        Assert.Equal(
            new[] { RepositoryArtifact.GitRepository, RepositoryArtifact.CSharpProject },
            repo.Artifacts);
        Assert.True(repo.IsGitRepository);
    }
}
