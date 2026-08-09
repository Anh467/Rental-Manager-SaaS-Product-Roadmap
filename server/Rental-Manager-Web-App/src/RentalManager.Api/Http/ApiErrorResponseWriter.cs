using System.Text.Json;
using RentalManager.Api.Middlewares;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Api.Http;

/// <summary>
/// Writes the <see cref="ApiErrorResponse"/> envelope to the wire. This is
/// the single serializer for error responses; nothing else in the API
/// should call <see cref="JsonSerializer"/> on an <see cref="ApiErrorResponse"/>.
/// </summary>
public sealed class ApiErrorResponseWriter : IApiErrorResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string messageKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyList<ApiFieldError>? fieldErrors = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureActive(messageKey);

        if (context.Response.HasStarted)
        {
            return;
        }

        string correlationId = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        // CorrelationIdMiddleware also sets this via OnStarting, but the
        // writer sets it directly too so the header always matches the body
        // even when the writer is exercised outside the full pipeline.
        context.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var payload = new ApiErrorResponse
        {
            MessageKey = messageKey,
            Parameters = parameters is { Count: > 0 } ? parameters : null,
            FieldErrors = fieldErrors is { Count: > 0 } ? fieldErrors : null,
            CorrelationId = correlationId
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, SerializerOptions),
            cancellationToken);
    }

    /// <summary>
    /// The catalog is the only source of truth for which keys the backend
    /// may emit. A deprecated (or unknown) key here is always a bug in the
    /// caller, never legitimate client input, so it fails loudly instead of
    /// shipping a key the client can no longer localize correctly.
    /// </summary>
    private static void EnsureActive(string messageKey)
    {
        if (!MessageCatalog.IsActive(messageKey))
        {
            throw new InvalidOperationException(
                $"Refusing to emit non-active message key '{messageKey}'.");
        }
    }
}
