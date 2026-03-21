using System.Reflection;
using AutoApi.Core.Abstractions;
using AutoApi.EFCore;

namespace AutoApi.Core.Tests;

public class AutoApiOptionsTests
{
    [Fact]
    public void DefaultRoutePrefix_IsApi()
    {
        var opts = new AutoApiOptions();
        Assert.Equal("api", opts.RoutePrefix);
    }

    [Fact]
    public void DefaultAssemblies_IsEmptyList()
    {
        var opts = new AutoApiOptions();
        Assert.NotNull(opts.Assemblies);
        Assert.Empty(opts.Assemblies);
    }

    [Fact]
    public void RoutePrefix_CanBeOverridden()
    {
        var opts = new AutoApiOptions { RoutePrefix = "v2/api" };
        Assert.Equal("v2/api", opts.RoutePrefix);
    }

    [Fact]
    public void RoutePrefix_CanBeEmpty()
    {
        var opts = new AutoApiOptions { RoutePrefix = "" };
        Assert.Equal("", opts.RoutePrefix);
    }

    [Fact]
    public void Assemblies_CanAddAssembly()
    {
        var opts = new AutoApiOptions();
        opts.Assemblies.Add(Assembly.GetExecutingAssembly());
        Assert.Single(opts.Assemblies);
    }

    [Fact]
    public void UseStore_AddsStoreOverrideEntry()
    {
        var opts = new AutoApiOptions();
        opts.UseStore<ModelWithId, StubStore>();

        Assert.Single(opts.StoreOverrides);
        Assert.Equal(typeof(ModelWithId), opts.StoreOverrides[0].ModelType);
        Assert.Equal(typeof(StubStore),   opts.StoreOverrides[0].StoreType);
    }

    [Fact]
    public void UseStore_ReturnsTheSameOptionsInstance_ForFluency()
    {
        var opts = new AutoApiOptions();
        var returned = opts.UseStore<ModelWithId, StubStore>();
        Assert.Same(opts, returned);
    }

    [Fact]
    public void UseStore_MultipleModels_AllRegistered()
    {
        var opts = new AutoApiOptions();
        opts.UseStore<ModelWithId, StubStore>()
            .UseStore<AnotherModel, AnotherStore>();

        Assert.Equal(2, opts.StoreOverrides.Count);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private class ModelWithId  { public int Id { get; set; } }
    private class AnotherModel { public int Id { get; set; } }

    private class StubStore : IAutoApiStore<ModelWithId>
    {
        public Task<IEnumerable<ModelWithId>> GetAllAsync()              => Task.FromResult(Enumerable.Empty<ModelWithId>());
        public Task<ModelWithId?> GetByIdAsync(int id)                   => Task.FromResult<ModelWithId?>(null);
        public Task<ModelWithId> CreateAsync(ModelWithId item)           => Task.FromResult(item);
        public Task<ModelWithId> UpdateAsync(int id, ModelWithId item)   => Task.FromResult(item);
        public Task DeleteAsync(int id)                                  => Task.CompletedTask;
    }

    private class AnotherStore : IAutoApiStore<AnotherModel>
    {
        public Task<IEnumerable<AnotherModel>> GetAllAsync()             => Task.FromResult(Enumerable.Empty<AnotherModel>());
        public Task<AnotherModel?> GetByIdAsync(int id)                  => Task.FromResult<AnotherModel?>(null);
        public Task<AnotherModel> CreateAsync(AnotherModel item)         => Task.FromResult(item);
        public Task<AnotherModel> UpdateAsync(int id, AnotherModel item) => Task.FromResult(item);
        public Task DeleteAsync(int id)                                  => Task.CompletedTask;
    }
}
