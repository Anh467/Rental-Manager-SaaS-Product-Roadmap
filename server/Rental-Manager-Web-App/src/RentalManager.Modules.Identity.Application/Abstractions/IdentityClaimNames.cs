namespace RentalManager.Modules.Identity.Application.Abstractions;

public static class IdentityClaimNames
{
    /// <summary>
    /// The internal <c>[dbo].[User].[Id]</c>. Deliberately not <c>sub</c>: the
    /// application cookie carries our own user id, while <c>sub</c> belongs to
    /// the external provider and only ever appears on the external principal.
    /// </summary>
    public const string UserId = "uid";

    /// <summary>
    /// The stable provider key the session was established with, kept for audit.
    /// It is never treated as an authorization input.
    /// </summary>
    public const string IdentityProvider = "idp";

    /// <summary>
    /// The user's security stamp as it was when the session was issued.
    /// </summary>
    public const string SecurityStamp = "sst";

    public const string Scope = "scope";

    public const string ActiveOrganizationId = "organization_id";

    /// <summary>
    /// Verified <c>[org].[StaffMembership].[Id]</c> for an organization-scoped
    /// session. Never accepted from the client as an authorization input.
    /// </summary>
    public const string StaffMembershipId = "staff_membership_id";

    public const string ScopeGlobal = "global";

    public const string ScopeOrganization = "organization";

    /// <summary>
    /// Authentication method reference recorded on the session. External
    /// federation only; a password-based value is never emitted.
    /// </summary>
    public const string AuthenticationMethodExternal = "external";
}
