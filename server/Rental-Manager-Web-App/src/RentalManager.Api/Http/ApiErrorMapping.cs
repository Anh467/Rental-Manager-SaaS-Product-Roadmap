using RentalManager.BuildingBlocks.Contracts;

namespace RentalManager.Api.Http;

/// <summary>
/// The HTTP status code and message key an exception maps to, plus whatever
/// safe parameters/field errors go with it. Never carries exception
/// messages, stack traces or other exception detail.
/// </summary>
public sealed record ApiErrorMapping(
    int StatusCode,
    string MessageKey,
    IReadOnlyDictionary<string, object?>? Parameters = null,
    IReadOnlyList<ApiFieldError>? FieldErrors = null);
