namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// Short-lived, data-protected proof that a user authenticated and still owes an
/// organization choice. It carries the provider so the session that is finally
/// issued records the same provider the user actually authenticated with.
/// </summary>
public sealed record OrganizationSelectionTicket(
    Guid UserId,
    string SecurityStamp,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? Provider = null);
