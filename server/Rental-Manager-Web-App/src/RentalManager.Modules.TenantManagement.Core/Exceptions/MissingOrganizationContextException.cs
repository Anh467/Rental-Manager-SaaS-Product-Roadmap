using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// Organization-owned data was accessed without a trusted organization
/// context. This is the application-level half of fail-closed isolation; the
/// database-level half is row level security. Maps to HTTP 403.
/// </summary>
public sealed class MissingOrganizationContextException : DomainException
{
    public MissingOrganizationContextException()
        : base(MessageCode.Error.OrganizationContextMissing)
    {
    }
}
