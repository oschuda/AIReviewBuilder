using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.Services;

namespace AIReviewBuilder.Tests.Domain;

public sealed class PathBoundaryGuardTests
{
    private static string Root => OperatingSystem.IsWindows() ? @"C:\repo" : "/repo";

    [Fact]
    public void ResolveWithinRoot_RelativeChild_Succeeds()
    {
        string resolved = PathBoundaryGuard.ResolveWithinRoot(Root, "src/Program.cs");
        Assert.StartsWith(Path.GetFullPath(Root), resolved, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../escape.cs")]
    [InlineData("../../etc/passwd")]
    [InlineData("src/../../escape.cs")]
    public void ResolveWithinRoot_TraversalEscape_Throws(string relative)
    {
        Assert.Throws<SecurityViolationException>(
            () => PathBoundaryGuard.ResolveWithinRoot(Root, relative));
    }

    [Fact]
    public void ResolveWithinRoot_AbsoluteInjection_Throws()
    {
        string absolute = OperatingSystem.IsWindows() ? @"C:\other\x.cs" : "/etc/passwd";
        Assert.Throws<SecurityViolationException>(
            () => PathBoundaryGuard.ResolveWithinRoot(Root, absolute));
    }

    [Fact]
    public void IsContained_SiblingPrefix_IsNotContained()
    {
        // "/repo-evil" shares a string prefix with "/repo" but is NOT inside it.
        string root = Path.GetFullPath(Root);
        string sibling = Path.GetFullPath(Root + "-evil");
        Assert.False(PathBoundaryGuard.IsContained(root, sibling));
    }

    [Fact]
    public void IsContained_RootItself_IsContained()
    {
        string root = Path.GetFullPath(Root);
        Assert.True(PathBoundaryGuard.IsContained(root, root));
    }
}
