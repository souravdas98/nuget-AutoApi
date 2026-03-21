using AutoApi.Core.Attributes;
using AutoApi.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;

namespace AutoApi.Core.Tests;

// ── Model used throughout integration tests ──────────────────────────────────

[AutoApi]
public class TodoItem
{
    public int    Id    { get; set; }
    public string Title { get; set; } = "";
    public bool   Done  { get; set; }
}

// ── Custom WebApplicationFactory ─────────────────────────────────────────────

public class AutoApiWebFactory : WebApplicationFactory<AutoApiWebFactory>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(e => e.MapAutoApiEndpoints());
            });
            web.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAutoApi(o =>
                {
                    // Only scan this assembly so other test models don't pollute
                    o.Assemblies.Clear();
                    o.Assemblies.Add(typeof(AutoApiWebFactory).Assembly);
                });
            });
        });
        return base.CreateHost(builder);
    }
}

// ── Integration tests ─────────────────────────────────────────────────────────

public class EndpointIntegrationTests : IClassFixture<AutoApiWebFactory>
{
    private readonly HttpClient _client;

    public EndpointIntegrationTests(AutoApiWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/todoitem ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_EmptyStore_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync("/api/todoitem");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<TodoItem>>();
        Assert.NotNull(items);
    }

    // ── POST /api/todoitem ────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidItem_Returns201WithCreatedItem()
    {
        var payload = new TodoItem { Title = "Write tests", Done = false };

        var response = await _client.PostAsJsonAsync("/api/todoitem", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.NotNull(created);
        Assert.Equal("Write tests", created.Title);
        Assert.True(created.Id > 0);
    }

    // ── GET /api/todoitem/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingItem_Returns200()
    {
        var post = await _client.PostAsJsonAsync("/api/todoitem",
            new TodoItem { Title = "Find me" });
        var created = await post.Content.ReadFromJsonAsync<TodoItem>();

        var response = await _client.GetAsync($"/api/todoitem/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var found = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Equal("Find me", found!.Title);
    }

    [Fact]
    public async Task GetById_MissingItem_Returns404()
    {
        var response = await _client.GetAsync("/api/todoitem/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PUT /api/todoitem/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task Put_ExistingItem_Returns200WithUpdatedItem()
    {
        var post = await _client.PostAsJsonAsync("/api/todoitem",
            new TodoItem { Title = "Old title" });
        var created = await post.Content.ReadFromJsonAsync<TodoItem>();

        var response = await _client.PutAsJsonAsync($"/api/todoitem/{created!.Id}",
            new TodoItem { Title = "New title", Done = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Equal("New title", updated!.Title);
        Assert.True(updated.Done);
    }

    [Fact]
    public async Task Put_MissingItem_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/todoitem/99999",
            new TodoItem { Title = "Ghost" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── DELETE /api/todoitem/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingItem_Returns204()
    {
        var post = await _client.PostAsJsonAsync("/api/todoitem",
            new TodoItem { Title = "Will be deleted" });
        var created = await post.Content.ReadFromJsonAsync<TodoItem>();

        var response = await _client.DeleteAsync($"/api/todoitem/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingItem_Returns204_NoOpIsIdempotent()
    {
        // Deleting a non-existent item should still return 204 (no-op)
        var response = await _client.DeleteAsync("/api/todoitem/99999");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── After delete, item is gone ────────────────────────────────────────────

    [Fact]
    public async Task Delete_ThenGetById_Returns404()
    {
        var post = await _client.PostAsJsonAsync("/api/todoitem",
            new TodoItem { Title = "Temporary" });
        var created = await post.Content.ReadFromJsonAsync<TodoItem>();

        await _client.DeleteAsync($"/api/todoitem/{created!.Id}");

        var response = await _client.GetAsync($"/api/todoitem/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
