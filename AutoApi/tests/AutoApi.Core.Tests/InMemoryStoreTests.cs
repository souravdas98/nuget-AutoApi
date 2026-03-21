using AutoApi.Core.Internal;

namespace AutoApi.Core.Tests;

public class InMemoryStoreTests
{
    private class Item
    {
        public int    Id   { get; set; }
        public string Name { get; set; } = "";
    }

    private static InMemoryStore<Item> NewStore() => new InMemoryStore<Item>();

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        var store = NewStore();
        var all = await store.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task GetAllAsync_AfterCreate_ReturnsAllItems()
    {
        var store = NewStore();
        await store.CreateAsync(new Item { Name = "A" });
        await store.CreateAsync(new Item { Name = "B" });

        var all = (await store.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AssignsIncrementalId()
    {
        var store = NewStore();
        var first  = await store.CreateAsync(new Item { Name = "X" });
        var second = await store.CreateAsync(new Item { Name = "Y" });

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
    }

    [Fact]
    public async Task CreateAsync_ReturnsTheCreatedItemWithId()
    {
        var store = NewStore();
        var item = await store.CreateAsync(new Item { Name = "Alpha" });

        Assert.Equal("Alpha", item.Name);
        Assert.True(item.Id > 0);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsItem()
    {
        var store = NewStore();
        var created = await store.CreateAsync(new Item { Name = "Test" });

        var found = await store.GetByIdAsync(created.Id);
        Assert.NotNull(found);
        Assert.Equal("Test", found.Name);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var store = NewStore();
        var found = await store.GetByIdAsync(999);
        Assert.Null(found);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingItem_UpdatesAndReturnItem()
    {
        var store = NewStore();
        var created = await store.CreateAsync(new Item { Name = "Old" });

        var updated = await store.UpdateAsync(created.Id, new Item { Name = "New" });

        Assert.Equal("New", updated.Name);
        Assert.Equal(created.Id, updated.Id);   // id must be preserved
    }

    [Fact]
    public async Task UpdateAsync_EnsuresIdNotOverwritten()
    {
        var store = NewStore();
        var created = await store.CreateAsync(new Item { Name = "Original" });

        // Send item with a wrong id in the body — should be corrected
        var updated = await store.UpdateAsync(created.Id, new Item { Id = 9999, Name = "Fixed" });
        Assert.Equal(created.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateAsync_MissingId_ThrowsKeyNotFoundException()
    {
        var store = NewStore();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => store.UpdateAsync(404, new Item { Name = "Ghost" }));
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingItem_RemovesIt()
    {
        var store = NewStore();
        var item = await store.CreateAsync(new Item { Name = "ToDelete" });

        await store.DeleteAsync(item.Id);

        Assert.Null(await store.GetByIdAsync(item.Id));
    }

    [Fact]
    public async Task DeleteAsync_MissingId_DoesNotThrow()
    {
        var store = NewStore();
        // Deleting a non-existent id should be a no-op
        var ex = await Record.ExceptionAsync(() => store.DeleteAsync(999));
        Assert.Null(ex);
    }

    // ── Model without Id property ────────────────────────────────────────────

    private class NoIdModel { public string Name { get; set; } = ""; }

    [Fact]
    public async Task CreateAsync_ModelWithoutId_ThrowsInvalidOperationException()
    {
        var store = new InMemoryStore<NoIdModel>();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.CreateAsync(new NoIdModel()));
    }
}
