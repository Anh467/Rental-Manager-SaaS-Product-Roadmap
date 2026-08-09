namespace RentalManager.Modules.Identity.Application.Abstractions;

public sealed record AuthenticatedIdentity(
    Guid UserId,
    string Email,
    string DisplayName,
    string SecurityStamp,
    Guid? GlobalRoleId,
    bool IsActive);
