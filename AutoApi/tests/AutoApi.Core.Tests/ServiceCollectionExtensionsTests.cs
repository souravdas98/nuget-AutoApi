using AutoApi.Core.Attributes;
using AutoApi.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AutoApi.Core.Tests;

public class ServiceCollectionExtensionsTests
{
    // ── Models ────────────────────────────────────────────────────────────────

    [AutoApi]
    private class WidgetModel { public int Id { get; set; } }

    // ── AddAutoApi ────────────────────────────────────────────────────────────

    [Fact]
    public void AddAutoApi_RegistersAutoApiOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoApi(o =>
        {
            o.Assemblies.Clear();
            o.Assemblies.Add(typeof(ServiceCollectionExtensionsTests).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetService<AutoApiOptions>();

        Assert.NotNull(opts);
    }

    [Fact]
    public void AddAutoApi_WithNoConfigureAction_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var ex = Record.Exception(() => services.AddAutoApi());
        Assert.Null(ex);
    }

    [Fact]
    public void AddAutoApi_RegistersInMemoryStore_ForDecoratedModel()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoApi(o =>
        {
            o.Assemblies.Clear();
            o.Assemblies.Add(typeof(ServiceCollectionExtensionsTests).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var store = provider.GetService<AutoApi.Core.Abstractions.IAutoApiStore<WidgetModel>>();
        Assert.NotNull(store);
    }

    [Fact]
    public void AddAutoApi_CustomStore_OverridesInMemoryStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoApi(o =>
        {
            o.Assemblies.Clear();
            o.Assemblies.Add(typeof(ServiceCollectionExtensionsTests).Assembly);
            o.UseStore<WidgetModel, CustomWidgetStore>();
        });

        var provider = services.BuildServiceProvider();
        var store = provider.GetService<AutoApi.Core.Abstractions.IAutoApiStore<WidgetModel>>();
        Assert.IsType<CustomWidgetStore>(store);
    }

    // ── MapAutoApiEndpoints ───────────────────────────────────────────────────

    [Fact]
    public void MapAutoApiEndpoints_DoesNotThrow_WhenNoModelsInAssembly()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAutoApi(o => o.Assemblies.Clear());  // empty scan
        var app = builder.Build();

        var ex = Record.Exception(() => app.MapAutoApiEndpoints());
        Assert.Null(ex);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private class CustomWidgetStore : AutoApi.Core.Abstractions.IAutoApiStore<WidgetModel>
    {
        public Task<IEnumerable<WidgetModel>> GetAllAsync()              => Task.FromResult(Enumerable.Empty<WidgetModel>());
        public Task<WidgetModel?> GetByIdAsync(int id)                   => Task.FromResult<WidgetModel?>(null);
        public Task<WidgetModel> CreateAsync(WidgetModel item)           => Task.FromResult(item);
        public Task<WidgetModel> UpdateAsync(int id, WidgetModel item)   => Task.FromResult(item);
        public Task DeleteAsync(int id)                                  => Task.CompletedTask;
    }
}
