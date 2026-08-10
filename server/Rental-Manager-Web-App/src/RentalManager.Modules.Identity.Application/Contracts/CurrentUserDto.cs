namespace RentalManager.Modules.Identity.Application.Contracts;

public sealed record CurrentUserDto(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    string Scope,
    Guid? OrganizationId,
    RoleSummaryDto? Role,
    IReadOnlyCollection<string> Permissions);
