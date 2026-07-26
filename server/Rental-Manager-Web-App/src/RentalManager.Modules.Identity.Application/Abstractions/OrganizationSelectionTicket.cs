namespace RentalManager.Modules.Identity.Application.Abstractions;

public sealed record OrganizationSelectionTicket(
    Guid UserId,
    string SecurityStamp,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
