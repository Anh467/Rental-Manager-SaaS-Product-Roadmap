using RentalManager.Api.Http;

namespace RentalManager.Api.Middlewares;

/// <summary>
/// Translates exceptions into the single error envelope. <see cref="IApiExceptionMapper"/>
/// maps the exception to an HTTP status and message key, and
/// <see cref="IApiErrorResponseWriter"/> is the only thing that serializes the
/// envelope, so no status-to-key mapping or serialization is duplicated here.
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    private readonly IApiExceptionMapper _mapper;
    private readonly IApiErrorResponseWriter _writer;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger,
        IApiExceptionMapper mapper,
        IApiErrorResponseWriter writer)
    {
        _next = next;
        _logger = logger;
        _mapper = mapper;
        _writer = writer;
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
        catch (Exception exception)
        {
            ApiErrorMapping mapping = _mapper.Map(exception);

            if (mapping.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                // Full exception detail (message, stack trace, inner
                // exceptions) is logged server-side only, keyed by
                // correlation id; it must never reach the response body.
                _logger.LogError(
                    exception,
                    "Unhandled exception for {Method} {Path}. CorrelationId {CorrelationId}.",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }

            await _writer.WriteAsync(
                context,
                mapping.StatusCode,
                mapping.MessageKey,
                mapping.Parameters,
                mapping.FieldErrors,
                context.RequestAborted);
        }
    }
}
