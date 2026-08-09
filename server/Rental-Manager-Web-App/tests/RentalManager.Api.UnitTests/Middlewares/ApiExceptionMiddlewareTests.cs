using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using RentalManager.Api.Http;
using RentalManager.Api.Middlewares;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;
using JsonException = System.Text.Json.JsonException;

namespace RentalManager.Api.UnitTests.Middlewares;

/// <summary>
/// End-to-end coverage of the exception middleware: an exception thrown by
/// the next delegate must come out as the correct status code, message key
/// and (when relevant) field errors, with the correlation id consistent
/// between the header and the body and no exception detail leaking out.
/// </summary>
public sealed class ApiExceptionMiddlewareTests
{
    [Theory]
    [MemberData(nameof(MappedExceptions))]
    public async Task Maps_exception_to_expected_status_and_message_key(
        Exception exception,
        int expectedStatusCode,
        string expectedMessageKey)
    {
        (int statusCode, JsonElement body, HttpContext context) = await InvokeAsync(exception);

        Assert.Equal(expectedStatusCode, statusCode);
        Assert.Equal(expectedMessageKey, body.GetProperty("messageKey").GetString());
        Assert.False(body.GetProperty("success").GetBoolean());

        string headerValue = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.Equal(context.TraceIdentifier, headerValue);
        Assert.Equal(headerValue, body.GetProperty("correlationId").GetString());
    }

    public static TheoryData<Exception, int, string> MappedExceptions()
    {
        return new TheoryData<Exception, int, string>
        {
            { new AntiforgeryValidationException("bad token"), StatusCodes.Status400BadRequest, MessageCode.Error.ValidationFailed },
            { new JsonException("bad json"), StatusCodes.Status400BadRequest, MessageCode.Error.ValidationFailed },
            { new BadHttpRequestException("malformed"), StatusCodes.Status400BadRequest, MessageCode.Error.ValidationFailed },
            { new AuthenticationFailedException(), StatusCodes.Status401Unauthorized, MessageCode.Error.AuthenticationRequired },
            { new PermissionDeniedException("update", "field"), StatusCodes.Status403Forbidden, MessageCode.Error.PermissionDenied },
            { new MissingOrganizationContextException(), StatusCodes.Status403Forbidden, MessageCode.Error.OrganizationContextMissing },
            { new ResourceNotFoundException("field"), StatusCodes.Status404NotFound, MessageCode.Error.NotFound },
            { new DuplicateResourceException("field"), StatusCodes.Status409Conflict, MessageCode.Error.AlreadyExists },
            { new ConcurrencyConflictException("field"), StatusCodes.Status409Conflict, MessageCode.Error.ConcurrencyConflict },
            { new ExternalServiceUnavailableException("objectStorage"), StatusCodes.Status503ServiceUnavailable, MessageCode.Error.ServiceUnavailable },
            { new InvalidOperationException("boom"), StatusCodes.Status500InternalServerError, MessageCode.Error.UnexpectedError }
        };
    }

    [Fact]
    public async Task Field_validation_failure_maps_to_422_with_field_errors()
    {
        var exception = new ValidationFailedException(
            [
                new ValidationFailure("key", MessageCode.Error.ValidationFailed),
                new ValidationFailure("name", MessageCode.Error.ValidationFailed)
            ]);

        (int statusCode, JsonElement body, _) = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, statusCode);
        Assert.Equal(MessageCode.Error.ValidationFailed, body.GetProperty("messageKey").GetString());

        JsonElement fieldErrors = body.GetProperty("fieldErrors");
        Assert.Equal(2, fieldErrors.GetArrayLength());
    }

    [Fact]
    public async Task Unmapped_domain_exception_is_500_not_409()
    {
        (int statusCode, JsonElement body, _) = await InvokeAsync(new UnmappedTestDomainException());

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.NotEqual(StatusCodes.Status409Conflict, statusCode);
        Assert.Equal(MessageCode.Error.UnexpectedError, body.GetProperty("messageKey").GetString());
    }

    [Fact]
    public async Task Business_rule_with_a_deprecated_key_never_reaches_the_client()
    {
        (int statusCode, JsonElement body, _) = await InvokeAsync(new BusinessRuleException("ERR-034"));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Equal(MessageCode.Error.UnexpectedError, body.GetProperty("messageKey").GetString());
        Assert.NotEqual("ERR-034", body.GetProperty("messageKey").GetString());
    }

    [Fact]
    public async Task Sensitive_exception_detail_never_appears_in_a_500_body()
    {
        const string secret = "Server=db;User Id=sa;Password=Sup3rSecret!;token=eyJhbGciOiJIUzI1NiJ9.secretpayload";
        var exception = new InvalidOperationException($"Connection failed: {secret}");

        (int statusCode, JsonElement body, _) = await InvokeAsync(exception);
        string raw = body.GetRawText();

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Equal(MessageCode.Error.UnexpectedError, body.GetProperty("messageKey").GetString());
        Assert.DoesNotContain("Sup3rSecret", raw);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9", raw);
        Assert.DoesNotContain("Connection failed", raw);
        Assert.DoesNotContain(nameof(InvalidOperationException), raw);
    }

    [Fact]
    public async Task Operation_canceled_by_the_client_is_swallowed_without_a_response()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        DefaultHttpContext context = new() { TraceIdentifier = "corr-cancelled" };
        context.Response.Body = new MemoryStream();
        context.RequestAborted = cts.Token;

        var middleware = new ApiExceptionMiddleware(
            _ => throw new OperationCanceledException(cts.Token),
            NullLogger<ApiExceptionMiddleware>.Instance,
            new ApiExceptionMapper(),
            new ApiErrorResponseWriter());

        await middleware.InvokeAsync(context);

        Assert.Equal(0, context.Response.Body.Length);
    }

    private static async Task<(int StatusCode, JsonElement Body, HttpContext Context)> InvokeAsync(
        Exception toThrow)
    {
        DefaultHttpContext context = new() { TraceIdentifier = $"corr-{Guid.NewGuid():N}" };
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionMiddleware(
            _ => throw toThrow,
            NullLogger<ApiExceptionMiddleware>.Instance,
            new ApiExceptionMapper(),
            new ApiErrorResponseWriter());

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string raw = await reader.ReadToEndAsync();
        JsonElement body = JsonDocument.Parse(raw).RootElement;

        return (context.Response.StatusCode, body, context);
    }

    private sealed class UnmappedTestDomainException()
        : DomainException("ERR-999-not-a-real-key");
}
