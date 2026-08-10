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

    [Fact]
    public void Empty_body_key_maps_to_400()
    {
        ActionContext context = CreateContext();
        context.ModelState.AddModelError(string.Empty, "A non-empty request body is required.");

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            ModelStateStatusResolver.Resolve(context));
    }

    [Fact]
    public void Present_value_errors_in_model_state_still_map_to_400()
    {
        // Semantic MaxLength/Required failures must not live in ModelState; if they
        // somehow appear, ModelState remains the 400 path (binding/basic only).
        ActionContext context = CreateContext();
        context.ModelState.SetModelValue(
            "name",
            new ValueProviderResult(new string('x', 300)));
        context.ModelState.AddModelError("name", "max length");

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
