using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Api.Http;
using RentalManager.Api.Middlewares;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace RentalManager.Api.UnitTests.Http;

/// <summary>
/// Program.cs wires the rate limiter and the cookie auth challenge/forbidden
/// events directly to <see cref="IApiErrorResponseWriter"/> rather than to
/// the exception middleware (there is no exception to catch in either case).
/// These tests reproduce the exact status/key pairs those call sites use, so
/// the single writer's contract is exercised the same way Program.cs uses it.
/// </summary>
public sealed class ProgramWiringTests
{
    private readonly ApiErrorResponseWriter _writer = new();

    [Fact]
    public async Task Rate_limit_rejection_is_429_ERR_040()
    {
        DefaultHttpContext context = CreateContext();

        await _writer.WriteAsync(
            context,
            StatusCodes.Status429TooManyRequests,
            MessageCode.Error.RateLimitExceeded);

        JsonElement body = await ReadBodyAsync(context);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.Equal(MessageCode.Error.RateLimitExceeded, body.GetProperty("messageKey").GetString());
        Assert.Equal(
            context.TraceIdentifier,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Cookie_challenge_redirect_to_login_is_401_ERR_003()
    {
        DefaultHttpContext context = CreateContext();

        await _writer.WriteAsync(
            context,
            StatusCodes.Status401Unauthorized,
            MessageCode.Error.AuthenticationRequired);

        JsonElement body = await ReadBodyAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal(MessageCode.Error.AuthenticationRequired, body.GetProperty("messageKey").GetString());
    }

    [Fact]
    public async Task Cookie_challenge_redirect_to_access_denied_is_403_ERR_004()
    {
        DefaultHttpContext context = CreateContext();

        await _writer.WriteAsync(
            context,
            StatusCodes.Status403Forbidden,
            MessageCode.Error.PermissionDenied);

        JsonElement body = await ReadBodyAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(MessageCode.Error.PermissionDenied, body.GetProperty("messageKey").GetString());
    }

    [Fact]
    public async Task Invalid_model_state_result_writes_via_the_shared_writer()
    {
        DefaultHttpContext context = CreateContext();
        var result = new ApiErrorActionResult(
            StatusCodes.Status400BadRequest,
            MessageCode.Error.ValidationFailed,
            [new ApiFieldError("key", MessageCode.Error.ValidationFailed)]);

        var services = new ServiceCollection();
        services.AddSingleton<IApiErrorResponseWriter>(_writer);
        context.RequestServices = services.BuildServiceProvider();

        var actionContext = new ActionContext(
            context,
            new RouteData(),
            new ActionDescriptor());

        await result.ExecuteResultAsync(actionContext);

        JsonElement body = await ReadBodyAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(MessageCode.Error.ValidationFailed, body.GetProperty("messageKey").GetString());
        Assert.Equal(1, body.GetProperty("fieldErrors").GetArrayLength());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext { TraceIdentifier = $"corr-{Guid.NewGuid():N}" };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string raw = await reader.ReadToEndAsync();
        return JsonDocument.Parse(raw).RootElement;
    }
}
