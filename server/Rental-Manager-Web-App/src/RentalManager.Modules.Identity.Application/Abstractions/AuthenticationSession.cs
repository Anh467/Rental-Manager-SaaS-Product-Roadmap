namespace RentalManager.Modules.Identity.Application.Abstractions;

public sealed record AuthenticationSession(
    Guid UserId,
    string Email,
    string DisplayName,
    string Scope,
    Guid? ActiveOrganizationId,
    string SecurityStamp,
    string? Provider = null,
    Guid? StaffMembershipId = null);
