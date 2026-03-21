using System;

namespace AutoApi.Core.Attributes;

/// <summary>
/// Marks a class for automatic CRUD API generation.
/// All endpoints (GET, POST, PUT, DELETE) are registered automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutoApiAttribute : Attribute
{
    /// <summary>
    /// Custom route prefix. Defaults to /api/{ClassName} (lowercase).
    /// Example: Route = "v1/products" → /v1/products
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Restrict which HTTP methods are exposed.
    /// Defaults to all: GET, POST, PUT, DELETE.
    /// Example: AllowedMethods = new[] { "GET" } for a read-only endpoint.
    /// </summary>
    public string[]? AllowedMethods { get; set; }

    /// <summary>
    /// Optional tag used for Swagger/OpenAPI grouping.
    /// Defaults to the class name.
    /// </summary>
    public string? Tag { get; set; }
}
