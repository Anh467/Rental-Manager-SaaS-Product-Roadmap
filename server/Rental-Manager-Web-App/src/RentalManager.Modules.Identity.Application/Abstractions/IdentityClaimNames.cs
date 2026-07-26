namespace RentalManager.Modules.Identity.Application.Abstractions;

public static class IdentityClaimNames
{
    public const string UserId = "sub";

    public const string Scope = "scope";

    public const string ActiveOrganizationId = "organization_id";

    public const string ScopeGlobal = "global";

    public const string ScopeOrganization = "organization";
}
