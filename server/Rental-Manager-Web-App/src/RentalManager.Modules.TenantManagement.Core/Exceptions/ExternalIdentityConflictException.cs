using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// A verified external identity cannot be mapped because the attribute it would
/// be provisioned with already belongs to a different account. The provider and
/// subject are deliberately not disclosed and the two accounts are never linked
/// automatically. Maps to HTTP 409.
/// </summary>
public sealed class ExternalIdentityConflictException : BusinessRuleException
{
    public ExternalIdentityConflictException(string objectName)
        : base(
            MessageCode.Error.ExternalIdentityConflict,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Object] = objectName
            })
    {
    }
}
