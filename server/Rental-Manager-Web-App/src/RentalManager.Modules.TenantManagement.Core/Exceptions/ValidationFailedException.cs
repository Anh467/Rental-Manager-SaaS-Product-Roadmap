using RentalManager.Modules.TenantManagement.Core.Constants;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// One request field failed validation. <paramref name="MessageKey"/> is a
/// canonical ERR code so the client can localize it.
/// </summary>
public sealed record ValidationFailure(
    string FieldKey,
    string MessageKey,
    IReadOnlyDictionary<string, object?>? Parameters = null);

/// <summary>
/// The request payload is invalid. Maps to HTTP 400.
/// </summary>
public sealed class ValidationFailedException : DomainException
{
    public ValidationFailedException(IReadOnlyCollection<ValidationFailure> failures)
        : base(MessageCode.Error.ValidationFailed)
    {
        ArgumentNullException.ThrowIfNull(failures);
        Failures = failures;
    }

    public ValidationFailedException(string fieldKey, string messageKey)
        : this([new ValidationFailure(fieldKey, messageKey)])
    {
    }

    public IReadOnlyCollection<ValidationFailure> Failures { get; }
}
