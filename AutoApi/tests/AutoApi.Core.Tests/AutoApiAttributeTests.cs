using AutoApi.Core.Attributes;

namespace AutoApi.Core.Tests;

public class AutoApiAttributeTests
{
    [Fact]
    public void Attribute_DefaultValues_AreNull()
    {
        var attr = new AutoApiAttribute();

        Assert.Null(attr.Route);
        Assert.Null(attr.AllowedMethods);
        Assert.Null(attr.Tag);
    }

    [Fact]
    public void Attribute_CanSetRoute()
    {
        var attr = new AutoApiAttribute { Route = "my/route" };
        Assert.Equal("my/route", attr.Route);
    }

    [Fact]
    public void Attribute_CanSetAllowedMethods()
    {
        var methods = new[] { "GET", "POST" };
        var attr = new AutoApiAttribute { AllowedMethods = methods };
        Assert.Equal(methods, attr.AllowedMethods);
    }

    [Fact]
    public void Attribute_CanSetTag()
    {
        var attr = new AutoApiAttribute { Tag = "Catalog" };
        Assert.Equal("Catalog", attr.Tag);
    }

    [Fact]
    public void Attribute_HasCorrectAttributeUsage()
    {
        var usage = (AttributeUsageAttribute)typeof(AutoApiAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)[0];

        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.Inherited);
    }

    [Fact]
    public void Attribute_IsSealed()
    {
        Assert.True(typeof(AutoApiAttribute).IsSealed);
    }

    // ── Applied to a real class ──────────────────────────────────────────────

    [AutoApi(Route = "items", Tag = "Test", AllowedMethods = new[] { "GET" })]
    private class DecoratedModel { public int Id { get; set; } }

    [Fact]
    public void Attribute_AppliedToClass_IsRetrievable()
    {
        var attr = (AutoApiAttribute)typeof(DecoratedModel)
            .GetCustomAttributes(typeof(AutoApiAttribute), false)[0];

        Assert.Equal("items", attr.Route);
        Assert.Equal("Test", attr.Tag);
        Assert.Equal(new[] { "GET" }, attr.AllowedMethods);
    }
}
