namespace RentalManager.BuildingBlocks.Contracts;

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

    /// <summary>
    /// Required so every envelope written to the wire can be correlated with
    /// server logs. Callers must always supply a non-empty value; use
    /// <c>HttpContext.TraceIdentifier</c> in controllers and middleware.
    /// </summary>
    public required string CorrelationId { get; init; }
}
