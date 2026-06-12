using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Infrastructure.Profiles;
using AIReviewBuilder.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIReviewBuilder.Tests.Infrastructure;

public sealed class JsonReviewProfileStoreTests
{
    private static JsonReviewProfileStore CreateStore(string dir) =>
        new(dir, NullLogger<JsonReviewProfileStore>.Instance);

    [Fact]
    public async Task GetAllAsync_EmptyStore_SeedsDefaultsAndPersists()
    {
        using var ws = new TempWorkspace();
        using var store = CreateStore(ws.Root);

        var profiles = await store.GetAllAsync(CancellationToken.None);

        Assert.Equal(DefaultReviewProfiles.All.Length, profiles.Length);
        Assert.True(File.Exists(Path.Combine(ws.Root, JsonReviewProfileStore.ProfilesFileName)));
    }

    [Fact]
    public async Task SaveAsync_ThenFindById_RoundTrips()
    {
        using var ws = new TempWorkspace();
        using var store = CreateStore(ws.Root);

        var custom = new ReviewProfile("custom", "Custom", ReviewProfileCategory.Architecture, "Look at layering.");
        await store.SaveAsync(custom, CancellationToken.None);

        ReviewProfile? found = await store.FindByIdAsync("custom", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Custom", found!.Name);
        Assert.Equal(ReviewProfileCategory.Architecture, found.Category);
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingId()
    {
        using var ws = new TempWorkspace();
        using var store = CreateStore(ws.Root);

        await store.SaveAsync(new ReviewProfile("p", "First", ReviewProfileCategory.General, "v1"), CancellationToken.None);
        await store.SaveAsync(new ReviewProfile("p", "Second", ReviewProfileCategory.General, "v2"), CancellationToken.None);

        ReviewProfile? found = await store.FindByIdAsync("p", CancellationToken.None);
        Assert.Equal("Second", found!.Name);
    }

    [Fact]
    public async Task FindByIdAsync_UnknownId_ReturnsNull()
    {
        using var ws = new TempWorkspace();
        using var store = CreateStore(ws.Root);

        ReviewProfile? found = await store.FindByIdAsync("missing", CancellationToken.None);
        Assert.Null(found);
    }
}
