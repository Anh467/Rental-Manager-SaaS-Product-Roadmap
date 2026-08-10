using RentalManager.BuildingBlocks.Contracts;

namespace RentalManager.Api.Http;

/// <summary>
/// The one place that serializes <see cref="ApiErrorResponse"/> onto the
/// wire. Every error path (exception middleware, model validation, rate
/// limiting, cookie auth challenges) must go through this writer instead of
/// building its own envelope.
/// </summary>
public interface IApiErrorResponseWriter
{
    Task WriteAsync(
        HttpContext context,
        int statusCode,
        string messageKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyList<ApiFieldError>? fieldErrors = null,
        CancellationToken cancellationToken = default);
}
