namespace RentalManager.Api.Contracts;

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

    public string? CorrelationId { get; init; }

    public static ApiResponse<T> Create(
        T? data,
        string messageKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            MessageKey = messageKey,
            Data = data,
            Parameters = parameters,
            CorrelationId = correlationId
        };
    }
}
