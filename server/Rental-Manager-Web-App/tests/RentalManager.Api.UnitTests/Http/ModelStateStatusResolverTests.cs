using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using RentalManager.Api.Http;
using Xunit;

namespace RentalManager.Api.UnitTests.Http;

public sealed class ModelStateStatusResolverTests
{
    [Fact]
    public void Missing_required_value_maps_to_400()
    {
        ActionContext context = CreateContext();
        context.ModelState.AddModelError("key", "required");

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            ModelStateStatusResolver.Resolve(context));
    }

    [Fact]
    public void Present_overlong_value_maps_to_422()
    {
        ActionContext context = CreateContext();
        string overlong = new('x', 300);
        context.ModelState.SetModelValue(
            "name",
            new ValueProviderResult(overlong));
        context.ModelState.AddModelError("name", "max length");

        Assert.Equal(
            StatusCodes.Status422UnprocessableEntity,
            ModelStateStatusResolver.Resolve(context));
    }

    [Fact]
    public void Present_whitespace_value_maps_to_422()
    {
        ActionContext context = CreateContext();
        context.ModelState.SetModelValue(
            "name",
            new ValueProviderResult("   "));
        context.ModelState.AddModelError("name", "invalid");

        Assert.Equal(
            StatusCodes.Status422UnprocessableEntity,
            ModelStateStatusResolver.Resolve(context));
    }

    [Fact]
    public void Binding_exception_maps_to_400()
    {
        ActionContext context = CreateContext();
        context.ModelState.TryAddModelException(
            "fieldTypeId",
            new FormatException("cannot convert"));

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            ModelStateStatusResolver.Resolve(context));
    }

    private static ActionContext CreateContext()
    {
        return new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
    }
}
