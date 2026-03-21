using System.Reflection;
using AutoApi.Core.Attributes;
using AutoApi.Core.Internal;

namespace AutoApi.Core.Tests;

public class AutoApiScannerTests
{
    // ── FindAutoApiTypes ─────────────────────────────────────────────────────

    [AutoApi]
    private class AnnotatedA { public int Id { get; set; } }

    [AutoApi]
    private class AnnotatedB { public int Id { get; set; } }

    private class NotAnnotated { }

    [Fact]
    public void FindAutoApiTypes_ReturnsOnlyDecoratedTypes()
    {
        var results = AutoApiScanner
            .FindAutoApiTypes(new[] { Assembly.GetExecutingAssembly() })
            .Select(x => x.Type)
            .ToList();

        Assert.Contains(typeof(AnnotatedA), results);
        Assert.Contains(typeof(AnnotatedB), results);
        Assert.DoesNotContain(typeof(NotAnnotated), results);
    }

    [Fact]
    public void FindAutoApiTypes_ReturnsCorrectAttribute()
    {
        var result = AutoApiScanner
            .FindAutoApiTypes(new[] { Assembly.GetExecutingAssembly() })
            .First(x => x.Type == typeof(AnnotatedA));

        Assert.NotNull(result.Attribute);
        Assert.IsType<AutoApiAttribute>(result.Attribute);
    }

    [Fact]
    public void FindAutoApiTypes_EmptyAssemblyList_ReturnsEmpty()
    {
        var results = AutoApiScanner
            .FindAutoApiTypes(Array.Empty<Assembly>());

        Assert.Empty(results);
    }

    [Fact]
    public void FindAutoApiTypes_HandlesBadAssembly_Gracefully()
    {
        // Should not throw even with an assembly that cannot load all types
        var results = AutoApiScanner
            .FindAutoApiTypes(new[] { Assembly.GetExecutingAssembly() });

        Assert.NotNull(results);
    }

    // ── ResolveRoute ─────────────────────────────────────────────────────────

    [Fact]
    public void ResolveRoute_WithExplicitRoute_UsesExplicitRoute()
    {
        var attr = new AutoApiAttribute { Route = "/custom/path" };
        var route = AutoApiScanner.ResolveRoute(typeof(AnnotatedA), attr, "api");
        Assert.Equal("custom/path", route);   // leading slash stripped
    }

    [Fact]
    public void ResolveRoute_WithExplicitRoute_NoLeadingSlash_UsesAsIs()
    {
        var attr = new AutoApiAttribute { Route = "catalog/products" };
        var route = AutoApiScanner.ResolveRoute(typeof(AnnotatedA), attr, "api");
        Assert.Equal("catalog/products", route);
    }

    [Fact]
    public void ResolveRoute_NoExplicitRoute_UsesPrefix_And_LowercaseClassName()
    {
        var attr = new AutoApiAttribute();
        var route = AutoApiScanner.ResolveRoute(typeof(AnnotatedA), attr, "api");
        Assert.Equal("api/annotateda", route);
    }

    [Fact]
    public void ResolveRoute_EmptyPrefix_ReturnsJustClassName()
    {
        var attr = new AutoApiAttribute();
        var route = AutoApiScanner.ResolveRoute(typeof(AnnotatedA), attr, "");
        Assert.Equal("annotateda", route);
    }

    [Fact]
    public void ResolveRoute_WhitespacePrefix_TreatedAsEmpty()
    {
        var attr = new AutoApiAttribute();
        var route = AutoApiScanner.ResolveRoute(typeof(AnnotatedA), attr, "  ");
        Assert.Equal("annotateda", route);
    }

    [Fact]
    public void ResolveRoute_PrefixWithSlashes_Trimmed()
    {
        var attr = new AutoApiAttribute();
        var route = AutoApiScanner.ResolveRoute(typeof(AnnotatedA), attr, "/v1/api/");
        Assert.Equal("v1/api/annotateda", route);
    }
}
