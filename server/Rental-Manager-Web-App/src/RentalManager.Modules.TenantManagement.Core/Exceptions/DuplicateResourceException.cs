using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// A uniqueness rule was violated, either by an application pre-check or by a
/// database unique constraint. Maps to HTTP 409.
/// </summary>
public sealed class DuplicateResourceException : DomainException
{
    public DuplicateResourceException(
        string objectName,
        string? fieldKey = null,
        Exception? innerException = null)
        : base(
            MessageCode.Error.AlreadyExists,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Object] = objectName
            },
            innerException)
    {
        FieldKey = fieldKey;
    }

    public string? FieldKey { get; }
}
