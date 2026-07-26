using RentalManager.Modules.TenantManagement.Core.Constants;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// The resource does not exist, or it belongs to another organization and must
/// stay indistinguishable from a missing resource. Maps to HTTP 404.
/// </summary>
public sealed class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string objectName)
        : base(
            MessageCode.Error.NotFound,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Object] = objectName
            })
    {
    }
}
