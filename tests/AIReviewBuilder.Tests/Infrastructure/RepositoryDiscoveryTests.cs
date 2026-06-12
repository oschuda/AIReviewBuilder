using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Infrastructure.Discovery;
using AIReviewBuilder.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIReviewBuilder.Tests.Infrastructure;

public sealed class RepositoryDiscoveryTests
{
    private static FileSystemRepositoryDiscoveryService CreateService() =>
        new(NullLogger<FileSystemRepositoryDiscoveryService>.Instance);

    [Fact]
    public async Task DiscoverAsync_DetectsMarkerArtifacts()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("repoA/MyApp.csproj", "<Project/>");
        ws.WriteFile("repoB/pyproject.toml", "[project]\n");
        ws.WriteFile("repoB/package.json", "{}");
        ws.WriteFile("plain/notes.txt", "nothing here");

        var result = await CreateService().DiscoverAsync(ws.Root, maxDepth: 5, CancellationToken.None);

        Assert.Equal(2, result.Length);

        DiscoveredRepository repoA = result.Single(r => r.Repository.Name == "repoA").Repository;
        Assert.Contains(RepositoryArtifact.CSharpProject, repoA.Artifacts);

        DiscoveredRepository repoB = result.Single(r => r.Repository.Name == "repoB").Repository;
        Assert.Contains(RepositoryArtifact.PythonProject, repoB.Artifacts);
        Assert.Contains(RepositoryArtifact.NodeProject, repoB.Artifacts);
    }

    [Fact]
    public async Task DiscoverAsync_StopsDescendingIntoRecognisedRepository()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("outer/MyApp.csproj", "<Project/>");
        ws.WriteFile("outer/nested/Nested.csproj", "<Project/>");

        var result = await CreateService().DiscoverAsync(ws.Root, maxDepth: 5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("outer", result[0].Repository.Name);
    }

    [Fact]
    public async Task DiscoverAsync_NonPositiveDepth_Throws()
    {
        using var ws = new TempWorkspace();
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateService().DiscoverAsync(ws.Root, maxDepth: 0, CancellationToken.None));
    }
}
