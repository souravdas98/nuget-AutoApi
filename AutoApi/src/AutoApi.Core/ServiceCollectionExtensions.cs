using System;
using System.Linq;
using System.Reflection;
using AutoApi.Core.Abstractions;
using AutoApi.Core.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AutoApi.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AutoApi services.
    /// Call in Program.cs: builder.Services.AddAutoApi()
    ///
    /// Options:
    ///   builder.Services.AddAutoApi(o => {
    ///       o.RoutePrefix = "v1/api";
    ///       o.Assemblies.Add(typeof(MyClass).Assembly);
    ///       o.UseStore&lt;Product, ProductEFStore&gt;();
    ///   });
    /// </summary>
    public static IServiceCollection AddAutoApi(
        this IServiceCollection services,
        Action<AutoApiOptions>? configure = null
    )
    {
        var options = new AutoApiOptions();

        // Default: scan the calling / entry assembly
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
            options.Assemblies.Add(entryAssembly);

        configure?.Invoke(options);

        // Store options for use during endpoint mapping
        services.AddSingleton(options);

        // Register stores: custom overrides first, then InMemoryStore as fallback
        var overrideMap = options.StoreOverrides.ToDictionary(x => x.ModelType, x => x.StoreType);

        var types = AutoApiScanner.FindAutoApiTypes(options.Assemblies);
        foreach (var (type, _) in types)
        {
            var storeInterface = typeof(IAutoApiStore<>).MakeGenericType(type);

            if (overrideMap.TryGetValue(type, out var storeImpl))
            {
                services.AddScoped(storeInterface, storeImpl);
            }
            else
            {
                var inMemory = typeof(InMemoryStore<>).MakeGenericType(type);
                services.AddSingleton(storeInterface, inMemory);
            }
        }

        return services;
    }
}

public static class WebApplicationExtensions
{
    /// <summary>
    /// Registers all [AutoApi]-decorated endpoints.
    /// Call in Program.cs after app.Build(): app.MapAutoApiEndpoints()
    /// </summary>
    public static IEndpointRouteBuilder MapAutoApiEndpoints(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<AutoApiOptions>();
        var types = AutoApiScanner.FindAutoApiTypes(options.Assemblies).ToList();

        foreach (var (type, attr) in types)
        {
            var route = AutoApiScanner.ResolveRoute(type, attr, options.RoutePrefix);
            var methods = attr.AllowedMethods ?? Array.Empty<string>();
            var tag = attr.Tag ?? type.Name;

            // Use reflection to call the generic Map<T> method
            var mapMethod = typeof(AutoApi.Core.Internal.EndpointBuilder)
                .GetMethod(
                    nameof(AutoApi.Core.Internal.EndpointBuilder.Map),
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
                )!
                .MakeGenericMethod(type);

            mapMethod.Invoke(null, new object[] { app, route, methods, tag });
        }

        return app;
    }
}
