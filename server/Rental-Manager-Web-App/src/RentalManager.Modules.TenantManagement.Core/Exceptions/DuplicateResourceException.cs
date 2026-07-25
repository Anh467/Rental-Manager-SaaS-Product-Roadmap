using RentalManager.Modules.TenantManagement.Core.Constants;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// A uniqueness rule was violated, either by an application pre-check or by a
/// database unique constraint. Maps to HTTP 409.
/// </summary>
public sealed class DuplicateResourceException : DomainException
{
    public DuplicateResourceException(string objectName, Exception? innerException = null)
        : base(
            MessageCode.Error.AlreadyExists,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Object] = objectName
            },
            innerException)
    {
    }
}
