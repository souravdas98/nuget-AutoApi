using System;
using System.Collections.Generic;
using System.Reflection;
using AutoApi.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AutoApi.Core;

/// <summary>
/// Configuration for AutoApi registration.
/// Pass to AddAutoApi(options => ...) in Program.cs.
/// </summary>
public class AutoApiOptions
{
    /// <summary>
    /// Global route prefix applied to all auto-generated endpoints.
    /// Default: "api"  →  /api/{classname}
    /// Set to "" for no prefix  →  /{classname}
    /// Set to "v2/api" for versioned  →  /v2/api/{classname}
    /// </summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// Assemblies to scan for [AutoApi] classes.
    /// Defaults to the entry assembly.
    /// </summary>
    public List<Assembly> Assemblies { get; set; } = new();

    internal List<(Type ModelType, Type StoreType)> StoreOverrides { get; } = new();

    /// <summary>
    /// Register a custom IAutoApiStore&lt;T&gt; for a specific model.
    /// Example: options.UseStore&lt;Product, ProductEFStore&gt;()
    /// </summary>
    public AutoApiOptions UseStore<TModel, TStore>()
        where TModel : class
        where TStore : class, IAutoApiStore<TModel>
    {
        StoreOverrides.Add((typeof(TModel), typeof(TStore)));
        return this;
    }
}
