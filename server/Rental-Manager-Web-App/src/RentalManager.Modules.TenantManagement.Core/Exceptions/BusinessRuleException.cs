namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// A business invariant was violated. Maps to HTTP 409.
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(
        string messageKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        Exception? innerException = null)
        : base(messageKey, parameters, innerException)
    {
    }
}
