using RentalManager.Api.Contracts.Common;

namespace RentalManager.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorAsync(context, exception);
        }
    }

    private static Task WriteErrorAsync(
        HttpContext context,
        Exception exception)
    {
        (int statusCode, string code) = exception switch
        {
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "resource.not_found"),

            ArgumentException =>
                (StatusCodes.Status400BadRequest, "request.invalid"),

            InvalidOperationException =>
                (StatusCodes.Status409Conflict, "operation.invalid"),

            _ =>
                (StatusCodes.Status500InternalServerError, "server.error")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(
            new ApiErrorResponse(
                code,
                statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Message,
                context.TraceIdentifier));
    }
}
