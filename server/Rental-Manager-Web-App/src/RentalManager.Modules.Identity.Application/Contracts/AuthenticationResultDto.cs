namespace RentalManager.Modules.Identity.Application.Contracts;

public sealed record AuthenticationResultDto(
    Guid Id,
    string Name,
    string Email,
    string Scope,
    Guid? OrganizationId,
    RoleSummaryDto? Role,
    IReadOnlyCollection<string> Permissions);
