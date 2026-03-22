using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoApi.Core.Attributes;

namespace AutoApi.Core.Internal;

internal static class AutoApiScanner
{
    /// <summary>
    /// Returns all types decorated with [AutoApi] from the provided assemblies.
    /// </summary>
    public static IEnumerable<(Type Type, AutoApiAttribute Attribute)> FindAutoApiTypes(
        IEnumerable<Assembly> assemblies
    )
    {
        return assemblies
            .Distinct()
            .SelectMany(SafeGetTypes)
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<AutoApiAttribute>()))
            .Where(x => x.Attr is not null)
            .Select(x => (x.Type, x.Attr!))
            .DistinctBy(x => x.Type);
    }

    /// <summary>
    /// Resolves the route for a type.
    /// Priority: explicit Route on attribute → global prefix + class name (lowercase).
    /// </summary>
    public static string ResolveRoute(Type type, AutoApiAttribute attr, string globalPrefix)
    {
        if (!string.IsNullOrWhiteSpace(attr.Route))
            return attr.Route.TrimStart('/');

        var prefix = globalPrefix.Trim('/');
        var name = type.Name.ToLowerInvariant();
        return string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
