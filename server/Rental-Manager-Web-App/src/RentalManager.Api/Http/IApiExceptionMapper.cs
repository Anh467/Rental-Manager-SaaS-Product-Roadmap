namespace RentalManager.Api.Http;

/// <summary>
/// Maps an exception to the HTTP status code and message key that
/// <see cref="IApiErrorResponseWriter"/> should emit. This is the only place
/// exception types are translated to HTTP semantics; no other file should
/// duplicate an exception-to-status mapping.
/// </summary>
public interface IApiExceptionMapper
{
    ApiErrorMapping Map(Exception exception);
}
