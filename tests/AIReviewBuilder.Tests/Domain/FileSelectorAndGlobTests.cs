using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.Services;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Tests.Domain;

public sealed class FileSelectorAndGlobTests
{
    [Theory]
    [InlineData("**/*.cs", "Program.cs", true)]
    [InlineData("**/*.cs", "src/app/Program.cs", true)]
    [InlineData("**/*.cs", "Program.py", false)]
    [InlineData("*.cs", "Program.cs", true)]
    [InlineData("*.cs", "src/Program.cs", false)]
    [InlineData("src/*.cs", "src/Program.cs", true)]
    [InlineData("src/?.cs", "src/A.cs", true)]
    [InlineData("src/?.cs", "src/AB.cs", false)]
    [InlineData("**/.git/**", ".git/config", true)]
    [InlineData("**/.git/**", "src/.git/config", true)]
    [InlineData("**/.git/**", "src/main.cs", false)]
    public void GlobPattern_Match(string pattern, string path, bool expected)
    {
        Assert.Equal(expected, GlobPattern.Parse(pattern).IsMatch(path));
    }

    [Fact]
    public void FileSelector_IncludeMatch_NoExclude_IsSelected()
    {
        var criteria = new FileSelectionCriteria(
            [GlobPattern.Parse("**/*.cs")],
            []);

        Assert.True(FileSelector.IsSelected("src/Program.cs", criteria));
        Assert.False(FileSelector.IsSelected("README.md", criteria));
    }

    [Fact]
    public void FileSelector_ExcludeWinsOverInclude()
    {
        var criteria = new FileSelectionCriteria(
            [GlobPattern.Parse("**/*.cs")],
            [GlobPattern.Parse("**/obj/**")]);

        Assert.True(FileSelector.IsSelected("src/Program.cs", criteria));
        Assert.False(FileSelector.IsSelected("src/obj/Generated.cs", criteria));
    }

    [Fact]
    public void FileSelector_Select_PreservesOrderAndDeduplicates()
    {
        var criteria = new FileSelectionCriteria(
            [GlobPattern.Parse("**/*.cs")],
            []);

        var input = new[] { "b.cs", "a.cs", "b.cs", "skip.md" };
        var result = FileSelector.Select(input, criteria);

        Assert.Equal(new[] { "b.cs", "a.cs" }, result);
    }

    [Fact]
    public void DefaultCriteria_ExcludesBuildOutputAndVcs()
    {
        FileSelectionCriteria criteria = FileSelectionCriteria.Default;

        Assert.True(FileSelector.IsSelected("src/Program.cs", criteria));
        Assert.False(FileSelector.IsSelected("bin/Debug/App.cs", criteria));
        Assert.False(FileSelector.IsSelected(".git/config", criteria));
        Assert.False(FileSelector.IsSelected("node_modules/pkg/index.json", criteria));
    }

    [Fact]
    public void FileSelectionCriteria_EmptyIncludes_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => new FileSelectionCriteria([], []));
    }
}
