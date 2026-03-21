using System;
using System.Collections.Generic;
using System.Linq;
using AutoApi.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AutoApi.Core.Internal;

internal static class EndpointBuilder
{
    private static readonly HashSet<string> AllMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "POST",
        "PUT",
        "DELETE",
    };

    /// <summary>
    /// Maps CRUD endpoints for type T onto the given route group.
    /// Respects AllowedMethods from the attribute.
    /// </summary>
    public static void Map<T>(
        IEndpointRouteBuilder app,
        string route,
        string[] allowedMethods,
        string tag
    )
        where T : class
    {
        var allowed =
            allowedMethods.Length > 0
                ? new HashSet<string>(allowedMethods, StringComparer.OrdinalIgnoreCase)
                : AllMethods;

        var group = app.MapGroup(route).WithTags(tag);

        // GET /route
        if (allowed.Contains("GET"))
        {
            group
                .MapGet(
                    "/",
                    async (IAutoApiStore<T> store) =>
                    {
                        var items = await store.GetAllAsync();
                        return Results.Ok(items);
                    }
                )
                .WithName($"GetAll{typeof(T).Name}")
                .WithSummary($"Get all {typeof(T).Name} records");

            // GET /route/{id}
            group
                .MapGet(
                    "/{id:int}",
                    async (int id, IAutoApiStore<T> store) =>
                    {
                        var item = await store.GetByIdAsync(id);
                        return item is null ? Results.NotFound() : Results.Ok(item);
                    }
                )
                .WithName($"Get{typeof(T).Name}ById")
                .WithSummary($"Get {typeof(T).Name} by id");
        }

        // POST /route
        if (allowed.Contains("POST"))
        {
            group
                .MapPost(
                    "/",
                    async (T item, IAutoApiStore<T> store) =>
                    {
                        var created = await store.CreateAsync(item);
                        return Results.Created($"/{route}/{GetId(created)}", created);
                    }
                )
                .WithName($"Create{typeof(T).Name}")
                .WithSummary($"Create a new {typeof(T).Name}");
        }

        // PUT /route/{id}
        if (allowed.Contains("PUT"))
        {
            group
                .MapPut(
                    "/{id:int}",
                    async (int id, T item, IAutoApiStore<T> store) =>
                    {
                        try
                        {
                            var updated = await store.UpdateAsync(id, item);
                            return Results.Ok(updated);
                        }
                        catch (KeyNotFoundException)
                        {
                            return Results.NotFound();
                        }
                    }
                )
                .WithName($"Update{typeof(T).Name}")
                .WithSummary($"Update {typeof(T).Name} by id");
        }

        // DELETE /route/{id}
        if (allowed.Contains("DELETE"))
        {
            group
                .MapDelete(
                    "/{id:int}",
                    async (int id, IAutoApiStore<T> store) =>
                    {
                        await store.DeleteAsync(id);
                        return Results.NoContent();
                    }
                )
                .WithName($"Delete{typeof(T).Name}")
                .WithSummary($"Delete {typeof(T).Name} by id");
        }
    }

    private static int GetId<T>(T item)
    {
        var prop = typeof(T).GetProperty("Id");
        return prop is not null ? (int)(prop.GetValue(item) ?? 0) : 0;
    }
}
