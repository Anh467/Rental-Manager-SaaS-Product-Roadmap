namespace RentalManager.Modules.Identity.Application.Abstractions;

public interface ICurrentIdentity
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    string? Scope { get; }

    Guid? ActiveOrganizationId { get; }

    Guid? StaffMembershipId { get; }
}
