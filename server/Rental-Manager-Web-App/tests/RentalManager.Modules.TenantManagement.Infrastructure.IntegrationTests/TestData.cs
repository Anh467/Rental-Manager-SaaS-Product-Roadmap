namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Fixed identities for the seeded tenants. Two organizations exist so every
/// isolation assertion has a concrete "other tenant" to be denied.
/// </summary>
internal static class TestData
{
    /// <summary>
    /// The provider key the seeded identity mappings use. It matches the default
    /// allow-list, so the tests exercise the same policy production does.
    /// </summary>
    public const string Provider = "oidc";

    public static class OrganizationA
    {
        public static readonly Guid Id =
            Guid.Parse("0a0a0a0a-0000-0000-0000-00000000000a");

        public static readonly Guid AdministratorRoleId =
            Guid.Parse("0a0a0a0a-0000-0000-0000-0000000000a1");

        public static readonly Guid ViewerRoleId =
            Guid.Parse("0a0a0a0a-0000-0000-0000-0000000000a2");
    }

    public static class OrganizationB
    {
        public static readonly Guid Id =
            Guid.Parse("0b0b0b0b-0000-0000-0000-00000000000b");

        public static readonly Guid AdministratorRoleId =
            Guid.Parse("0b0b0b0b-0000-0000-0000-0000000000b1");
    }

    public static class Users
    {
        /// <summary>Every field permission in organization A.</summary>
        public static readonly Guid AdministratorAId =
            Guid.Parse("11111111-0000-0000-0000-000000000001");

        public const string AdministratorAEmail = "admin.a@rentalmanager.test";

        public const string AdministratorASubject = "administrator-a-subject";

        /// <summary>Only <c>field.view</c> in organization A.</summary>
        public static readonly Guid ViewerAId =
            Guid.Parse("11111111-0000-0000-0000-000000000002");

        public const string ViewerAEmail = "viewer.a@rentalmanager.test";

        public const string ViewerASubject = "viewer-a-subject";

        /// <summary>Every field permission, but in organization B.</summary>
        public static readonly Guid AdministratorBId =
            Guid.Parse("11111111-0000-0000-0000-000000000003");

        public const string AdministratorBEmail = "admin.b@rentalmanager.test";

        public const string AdministratorBSubject = "administrator-b-subject";

        public static readonly Guid AdministratorAIdentityId =
            Guid.Parse("22222222-0000-0000-0000-000000000001");

        public static readonly Guid ViewerAIdentityId =
            Guid.Parse("22222222-0000-0000-0000-000000000002");

        public static readonly Guid AdministratorBIdentityId =
            Guid.Parse("22222222-0000-0000-0000-000000000003");
    }
}
