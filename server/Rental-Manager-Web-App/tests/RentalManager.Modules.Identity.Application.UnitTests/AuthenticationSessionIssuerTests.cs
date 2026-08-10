using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Authentication;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using Xunit;

namespace RentalManager.Modules.Identity.Application.UnitTests;

public sealed class AuthenticationSessionIssuerTests
{
    [Fact]
    public async Task Platform_role_issues_global_session_even_when_global_role_id_is_null()
    {
        var identity = CreateIdentity(globalRoleId: null);
        var sessions = new RecordingSessionWriter();
        var platform = new FixedPlatformPermissionReader(hasRole: true, ["platform.users.view"]);

        AuthenticationSessionIssuer issuer = CreateIssuer(
            platform,
            sessions,
            memberships: new EmptyMembershipReader());

        LoginResult result = await issuer.IssueAsync(identity, "oidc", CancellationToken.None);

        Assert.Equal(LoginStatus.Authenticated, result.Status);
        Assert.Equal(IdentityClaimNames.ScopeGlobal, Assert.Single(sessions.Sessions).Scope);
        Assert.Null(result.Authentication!.OrganizationId);
    }

    [Fact]
    public async Task Global_role_id_alone_does_not_issue_global_session()
    {
        Guid organizationId = Guid.CreateVersion7();
        Guid staffMembershipId = Guid.CreateVersion7();
        var identity = CreateIdentity(globalRoleId: Guid.CreateVersion7());
        var sessions = new RecordingSessionWriter();
        var memberships = new FixedMembershipReader(organizationId, staffMembershipId);

        AuthenticationSessionIssuer issuer = CreateIssuer(
            new FixedPlatformPermissionReader(hasRole: false, []),
            sessions,
            memberships);

        LoginResult result = await issuer.IssueAsync(identity, "oidc", CancellationToken.None);

        Assert.Equal(LoginStatus.Authenticated, result.Status);
        Assert.Equal(
            IdentityClaimNames.ScopeOrganization,
            Assert.Single(sessions.Sessions).Scope);
        Assert.Equal(organizationId, result.Authentication!.OrganizationId);
    }

    [Fact]
    public async Task Global_profile_permissions_come_only_from_platform_roles()
    {
        var identity = CreateIdentity(globalRoleId: Guid.CreateVersion7());
        var platform = new FixedPlatformPermissionReader(
            hasRole: true,
            ["platform.users.view", "platform.users.manage"]);

        AuthenticationSessionIssuer issuer = CreateIssuer(
            platform,
            new RecordingSessionWriter(),
            new EmptyMembershipReader());

        LoginResult result = await issuer.IssueAsync(identity, "oidc", CancellationToken.None);

        Assert.Equal(LoginStatus.Authenticated, result.Status);
        Assert.Equal(
            ["platform.users.manage", "platform.users.view"],
            result.Authentication!.Permissions.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal("PLATFORM_SUPER_ADMIN", result.Authentication.Role!.Key);
    }

    private static AuthenticatedIdentity CreateIdentity(Guid? globalRoleId) =>
        new(
            Guid.CreateVersion7(),
            "admin@example.com",
            "Admin",
            Guid.NewGuid().ToString("N"),
            globalRoleId,
            IsActive: true);

    private static AuthenticationSessionIssuer CreateIssuer(
        IPlatformPermissionReader platform,
        RecordingSessionWriter sessions,
        IOrganizationMembershipReader memberships)
    {
        var accessor = new OrganizationContextAccessor();
        var profileBuilder = new AuthenticationProfileBuilder(
            accessor,
            new EmptyPermissionReader(),
            platform);

        return new AuthenticationSessionIssuer(
            memberships,
            new UnusedTicketService(),
            sessions,
            profileBuilder,
            platform,
            new NoopSecurityEvents(),
            new EmptyOrganizationContext(),
            TimeProvider.System);
    }

    private sealed class FixedPlatformPermissionReader(
        bool hasRole,
        IEnumerable<string> keys) : IPlatformPermissionReader
    {
        private readonly HashSet<string> _keys = new(keys, StringComparer.Ordinal);

        public Task<bool> HasAnyPlatformRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(hasRole);

        public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(_keys);

        public Task<PlatformRoleSummary?> GetPrimaryRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformRoleSummary?>(
                hasRole
                    ? new PlatformRoleSummary("PLATFORM_SUPER_ADMIN", "Platform Super Admin")
                    : null);
    }

    private sealed class EmptyPermissionReader : IPermissionReader
    {
        public Task<bool> IsActiveMemberAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
    }

    private sealed class EmptyMembershipReader : IOrganizationMembershipReader
    {
        public Task<IReadOnlyList<OrganizationOptionDto>> ListActiveOrganizationsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationOptionDto>>([]);

        public Task<bool> IsActiveMemberAsync(
            Guid userId,
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Guid?> GetActiveStaffMembershipIdAsync(
            Guid userId,
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);
    }

    private sealed class FixedMembershipReader(Guid organizationId, Guid staffMembershipId)
        : IOrganizationMembershipReader
    {
        public Task<IReadOnlyList<OrganizationOptionDto>> ListActiveOrganizationsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationOptionDto>>(
            [
                new OrganizationOptionDto(organizationId, "Org")
            ]);

        public Task<bool> IsActiveMemberAsync(
            Guid userId,
            Guid requestedOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(requestedOrganizationId == organizationId);

        public Task<Guid?> GetActiveStaffMembershipIdAsync(
            Guid userId,
            Guid requestedOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(
                requestedOrganizationId == organizationId ? staffMembershipId : null);
    }

    private sealed class RecordingSessionWriter : IAuthenticationSessionWriter
    {
        public List<AuthenticationSession> Sessions { get; } = [];

        public Task WriteAsync(
            AuthenticationSession session,
            CancellationToken cancellationToken = default)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UnusedTicketService : IProtectedOrganizationSelectionTicketService
    {
        public string Protect(OrganizationSelectionTicket ticket) =>
            throw new NotSupportedException();

        public OrganizationSelectionTicket? Unprotect(string protectedTicket) => null;
    }

    private sealed class EmptyOrganizationContext : IOrganizationContext
    {
        public Guid? OrganizationId => null;

        public Guid? UserId => null;

        public Guid? StaffMembershipId => null;

        public string? CorrelationId => "test";

        public bool HasOrganization => false;
    }

    private sealed class NoopSecurityEvents : ISecurityEventPublisher
    {
        public Task PublishAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
