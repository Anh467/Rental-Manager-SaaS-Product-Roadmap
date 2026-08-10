using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// The resource exists but is deactivated, so the requested operation is
/// refused in that state. Reactivation is always an explicit, separately
/// authorized action. Maps to HTTP 409.
/// </summary>
public sealed class InactiveResourceException : BusinessRuleException
{
    public InactiveResourceException(string objectName)
        : base(
            MessageCode.Error.Inactive,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Object] = objectName
            })
    {
    }
}
