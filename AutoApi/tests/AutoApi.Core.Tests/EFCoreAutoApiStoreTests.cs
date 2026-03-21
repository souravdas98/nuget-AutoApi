using AutoApi.EFCore;
using Microsoft.EntityFrameworkCore;

namespace AutoApi.Core.Tests;

public class EFCoreAutoApiStoreTests : IDisposable
{
    // ── EF Core in-memory setup ──────────────────────────────────────────────

    private class Product
    {
        public int    Id    { get; set; }
        public string Name  { get; set; } = "";
        public decimal Price { get; set; }
    }

    private class NoIdEntity { public string Name { get; set; } = ""; }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<Product> Products => Set<Product>();
    }

    private class NoIdDbContext : DbContext
    {
        public NoIdDbContext(DbContextOptions<NoIdDbContext> options) : base(options) { }
        public DbSet<NoIdEntity> Entities => Set<NoIdEntity>();
    }

    private readonly TestDbContext _db;
    private readonly EFCoreAutoApiStore<Product, TestDbContext> _store;

    public EFCoreAutoApiStoreTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())   // isolated per test
            .Options;

        _db    = new TestDbContext(options);
        _store = new EFCoreAutoApiStore<Product, TestDbContext>(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoData_ReturnsEmpty()
    {
        var all = await _store.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsAll()
    {
        _db.Products.AddRange(
            new Product { Name = "A", Price = 1 },
            new Product { Name = "B", Price = 2 });
        await _db.SaveChangesAsync();

        var all = (await _store.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProduct()
    {
        var p = new Product { Name = "Widget", Price = 9.99m };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        var found = await _store.GetByIdAsync(p.Id);
        Assert.NotNull(found);
        Assert.Equal("Widget", found.Name);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var found = await _store.GetByIdAsync(9999);
        Assert.Null(found);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsItem()
    {
        var created = await _store.CreateAsync(new Product { Name = "Gadget", Price = 49.99m });

        Assert.True(created.Id > 0);
        Assert.Equal("Gadget", created.Name);
        Assert.Equal(1, _db.Products.Count());
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesFields()
    {
        var p = new Product { Name = "Old", Price = 1 };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        var updated = await _store.UpdateAsync(p.Id, new Product { Name = "New", Price = 99 });

        Assert.Equal("New",  updated.Name);
        Assert.Equal(99,     updated.Price);
        Assert.Equal(p.Id,   updated.Id);     // id must not change
    }

    [Fact]
    public async Task UpdateAsync_PreservesId_EvenIfBodySendsWrongId()
    {
        var p = new Product { Name = "KeepId" };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        var updated = await _store.UpdateAsync(p.Id, new Product { Id = 9999, Name = "Fixed" });
        Assert.Equal(p.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateAsync_MissingId_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _store.UpdateAsync(404, new Product { Name = "Ghost" }));
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingProduct_RemovesIt()
    {
        var p = new Product { Name = "ToRemove" };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        await _store.DeleteAsync(p.Id);

        Assert.Equal(0, _db.Products.Count());
    }

    [Fact]
    public async Task DeleteAsync_MissingId_IsNoOp()
    {
        var ex = await Record.ExceptionAsync(() => _store.DeleteAsync(9999));
        Assert.Null(ex);
    }

    // ── Model without int Id ─────────────────────────────────────────────────

    [Fact]
    public async Task Constructor_ModelWithoutIntId_ThrowsOnFirstAccess()
    {
        var opts = new DbContextOptionsBuilder<NoIdDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db    = new NoIdDbContext(opts);
        var badStore = new EFCoreAutoApiStore<NoIdEntity, NoIdDbContext>(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => badStore.GetByIdAsync(1));
    }
}
