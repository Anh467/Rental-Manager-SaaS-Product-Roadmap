using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// The caller is authenticated but lacks the required permission.
/// Maps to HTTP 403.
/// </summary>
public sealed class PermissionDeniedException : DomainException
{
    public PermissionDeniedException(string action, string objectName)
        : base(
            MessageCode.Error.PermissionDenied,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Action] = action,
                [MessageCode.Parameter.Object] = objectName
            })
    {
    }
}
