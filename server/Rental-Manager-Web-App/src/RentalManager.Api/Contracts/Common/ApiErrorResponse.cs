namespace RentalManager.Api.Contracts.Common;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    string? TraceId = null);
