using Microsoft.AspNetCore.Antiforgery;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using JsonException = System.Text.Json.JsonException;

namespace RentalManager.Api.Http;

/// <summary>
/// The single exception-to-HTTP mapping table for the API. Everything here
/// mirrors the message catalog: statuses come from the exception type, keys
/// come from the exception (falling back to a safe default when a key isn't
/// active), and nothing here ever forwards exception text to the client.
/// </summary>
public sealed class ApiExceptionMapper : IApiExceptionMapper
{
    public ApiErrorMapping Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            ValidationFailedException ex => MapValidationFailed(ex),
            AuthenticationFailedException ex => new ApiErrorMapping(
                StatusCodes.Status401Unauthorized,
                ex.MessageKey,
                NonEmpty(ex.Parameters)),
            MissingOrganizationContextException ex => new ApiErrorMapping(
                StatusCodes.Status403Forbidden,
                ex.MessageKey,
                NonEmpty(ex.Parameters)),
            PermissionDeniedException ex => new ApiErrorMapping(
                StatusCodes.Status403Forbidden,
                ex.MessageKey,
                NonEmpty(ex.Parameters)),
            ResourceNotFoundException ex => new ApiErrorMapping(
                StatusCodes.Status404NotFound,
                ex.MessageKey,
                NonEmpty(ex.Parameters)),
            DuplicateResourceException ex => MapDuplicateResource(ex),
            ConcurrencyConflictException ex => new ApiErrorMapping(
                StatusCodes.Status409Conflict,
                ex.MessageKey,
                NonEmpty(ex.Parameters)),
            ExternalServiceUnavailableException ex => new ApiErrorMapping(
                StatusCodes.Status503ServiceUnavailable,
                MessageCode.Error.ServiceUnavailable,
                NonEmpty(ex.Parameters)),
            BusinessRuleException ex => MapBusinessRule(ex),
            AntiforgeryValidationException => new ApiErrorMapping(
                StatusCodes.Status400BadRequest,
                MessageCode.Error.ValidationFailed),
            JsonException => new ApiErrorMapping(
                StatusCodes.Status400BadRequest,
                MessageCode.Error.ValidationFailed),
            BadHttpRequestException => new ApiErrorMapping(
                StatusCodes.Status400BadRequest,
                MessageCode.Error.ValidationFailed),
            // Any DomainException not covered by a specific case above is an
            // unmapped programming/config error, not a client-facing
            // conflict, so it is a 500 rather than defaulting to 409.
            DomainException => new ApiErrorMapping(
                StatusCodes.Status500InternalServerError,
                MessageCode.Error.UnexpectedError),
            _ => new ApiErrorMapping(
                StatusCodes.Status500InternalServerError,
                MessageCode.Error.UnexpectedError)
        };
    }

    private static ApiErrorMapping MapValidationFailed(ValidationFailedException exception)
    {
        IReadOnlyList<ApiFieldError> fieldErrors = exception.Failures
            .Select(failure => new ApiFieldError(
                JsonPropertyPathMapper.ToCamelCasePath(failure.FieldKey),
                failure.MessageKey,
                failure.Parameters))
            .ToArray();

        return new ApiErrorMapping(
            StatusCodes.Status422UnprocessableEntity,
            exception.MessageKey,
            NonEmpty(exception.Parameters),
            fieldErrors);
    }

    private static ApiErrorMapping MapDuplicateResource(DuplicateResourceException exception)
    {
        string messageKey = MessageCatalog.IsActive(exception.MessageKey)
            ? exception.MessageKey
            : MessageCode.Error.AlreadyExists;

        IReadOnlyList<ApiFieldError>? fieldErrors = exception.FieldKey is null
            ? null
            : [new ApiFieldError(
                JsonPropertyPathMapper.ToCamelCasePath(exception.FieldKey),
                messageKey,
                exception.Parameters)];

        return new ApiErrorMapping(
            StatusCodes.Status409Conflict,
            messageKey,
            NonEmpty(exception.Parameters),
            fieldErrors);
    }

    private static ApiErrorMapping MapBusinessRule(BusinessRuleException exception)
    {
        if (MessageCatalog.IsActive(exception.MessageKey))
        {
            return new ApiErrorMapping(
                StatusCodes.Status409Conflict,
                exception.MessageKey,
                NonEmpty(exception.Parameters));
        }

        // A business rule carrying a key the catalog doesn't recognize as
        // active is a programming error, not a safe-to-disclose conflict.
        return new ApiErrorMapping(
            StatusCodes.Status500InternalServerError,
            MessageCode.Error.UnexpectedError);
    }

    private static IReadOnlyDictionary<string, object?>? NonEmpty(
        IReadOnlyDictionary<string, object?>? parameters) =>
        parameters is { Count: > 0 } ? parameters : null;
}
