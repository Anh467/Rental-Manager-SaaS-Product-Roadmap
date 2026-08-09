using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using RentalManager.Api.Contracts;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Api.Middlewares;

/// <summary>
/// Translates exceptions into the single error envelope. The HTTP status comes
/// from the exception type and the message key comes from the exception, so no
/// status-to-key mapping is duplicated in controllers.
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller went away; there is nobody left to send a response to.
        }
        catch (ValidationFailedException exception)
        {
            await WriteAsync(
                context,
                HttpStatusCode.BadRequest,
                exception.MessageKey,
                exception.Parameters,
                exception.Failures
                    .Select(failure => new ApiFieldError(
                        JsonPropertyPathMapper.ToCamelCasePath(failure.FieldKey),
                        failure.MessageKey,
                        failure.Parameters))
                    .ToArray());
        }
        catch (AuthenticationFailedException exception)
        {
            await WriteAsync(
                context,
                HttpStatusCode.Unauthorized,
                exception.MessageKey,
                exception.Parameters);
        }
        catch (AntiforgeryValidationException)
        {
            await WriteAsync(
                context,
                HttpStatusCode.BadRequest,
                MessageCode.Error.ValidationFailed,
                parameters: null);
        }
        catch (ResourceNotFoundException exception)
        {
            await WriteAsync(
                context,
                HttpStatusCode.NotFound,
                exception.MessageKey,
                exception.Parameters);
        }
        catch (MissingOrganizationContextException exception)
        {
            await WriteAsync(
                context,
                HttpStatusCode.Forbidden,
                exception.MessageKey,
                exception.Parameters);
        }
        catch (PermissionDeniedException exception)
        {
            await WriteAsync(
                context,
                HttpStatusCode.Forbidden,
                exception.MessageKey,
                exception.Parameters);
        }
        catch (DuplicateResourceException exception)
        {
            await WriteAsync(
                context,
                HttpStatusCode.Conflict,
                exception.MessageKey,
                exception.Parameters,
                exception.FieldKey is null
                    ? null
                    : [new ApiFieldError(
                        JsonPropertyPathMapper.ToCamelCasePath(exception.FieldKey),
                        exception.MessageKey,
                        exception.Parameters)]);
        }
        catch (DomainException exception)
        {
            // Duplicate, stale row version and invariant violations all describe
            // a conflict with the current state of the resource.
            await WriteAsync(
                context,
                HttpStatusCode.Conflict,
                exception.MessageKey,
                exception.Parameters);
        }
        catch (Exception exception)
        {
            string correlationId = ResolveCorrelationId(context);

            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. CorrelationId {CorrelationId}.",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await WriteAsync(
                context,
                HttpStatusCode.InternalServerError,
                MessageCode.Error.UnexpectedError,
                parameters: null);
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string messageKey,
        IReadOnlyDictionary<string, object?>? parameters,
        IReadOnlyList<ApiFieldError>? fieldErrors = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new ApiErrorResponse
        {
            MessageKey = messageKey,
            Parameters = parameters is { Count: > 0 } ? parameters : null,
            FieldErrors = fieldErrors is { Count: > 0 } ? fieldErrors : null,
            CorrelationId = ResolveCorrelationId(context)
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, SerializerOptions),
            context.RequestAborted);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static string ResolveCorrelationId(HttpContext context)
    {
        return context.TraceIdentifier;
    }

}
