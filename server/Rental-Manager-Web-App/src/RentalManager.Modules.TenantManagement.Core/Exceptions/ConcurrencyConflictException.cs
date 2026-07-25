using RentalManager.Modules.TenantManagement.Core.Constants;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// The supplied row version no longer matches the stored row version.
/// Maps to HTTP 409.
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string objectName)
        : base(
            MessageCode.Error.ConcurrencyConflict,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Object] = objectName
            })
    {
    }
}
