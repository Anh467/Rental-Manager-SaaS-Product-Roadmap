namespace RentalManager.BuildingBlocks.Contracts;

/// <summary>
/// Success envelope. Shape is dictated by the client contract: a message key
/// rather than text, so the client owns localization.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success => true;

    public required string MessageKey { get; init; }

    public T? Data { get; init; }

    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// Required so every envelope written to the wire can be correlated with
    /// server logs. Callers must always supply a non-empty value; use
    /// <c>HttpContext.TraceIdentifier</c> in controllers and middleware.
    /// </summary>
    public required string CorrelationId { get; init; }

    public static ApiResponse<T> Create(
        T? data,
        string messageKey,
        string correlationId,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new ApiResponse<T>
        {
            MessageKey = messageKey,
            Data = data,
            Parameters = parameters,
            CorrelationId = correlationId
        };
    }
}
