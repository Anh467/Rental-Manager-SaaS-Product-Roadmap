namespace RentalManager.Api.Contracts;

/// <summary>
/// One invalid request field, keyed the way the client's form state is keyed.
/// </summary>
public sealed record ApiFieldError(
    string FieldKey,
    string MessageKey,
    IReadOnlyDictionary<string, object?>? Parameters = null);

/// <summary>
/// Error envelope. The single error contract for this API; there is no parallel
/// ProblemDetails representation.
/// </summary>
public sealed class ApiErrorResponse
{
    public bool Success => false;

    public required string MessageKey { get; init; }

    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    public IReadOnlyList<ApiFieldError>? FieldErrors { get; init; }

    public string? CorrelationId { get; init; }
}
