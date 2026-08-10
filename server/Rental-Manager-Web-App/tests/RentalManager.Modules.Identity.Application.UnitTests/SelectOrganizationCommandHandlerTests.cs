using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Authentication;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Authentication.SelectOrganization;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;

namespace RentalManager.Modules.Identity.Application.UnitTests;

public sealed class SelectOrganizationCommandHandlerTests
{
    [Fact]
    public async Task Select_rejects_organization_client_does_not_belong_to()
    {
        Guid userId = Guid.CreateVersion7();
        var stamp = Guid.NewGuid().ToString("N");
        var belongingOrg = Guid.CreateVersion7();
        var foreignOrg = Guid.CreateVersion7();

        SelectOrganizationCommandHandler handler = CreateHandler(
            userId,
            stamp,
            belongingOrg,
            Guid.CreateVersion7(),
            out _);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            handler.HandleAsync(
                new SelectOrganizationCommand("ticket", foreignOrg),
                CancellationToken.None));
    }

    [Fact]
    public async Task Select_issues_session_with_verified_staff_membership_id()
    {
        Guid userId = Guid.CreateVersion7();
        Guid organizationId = Guid.CreateVersion7();
        Guid staffMembershipId = Guid.CreateVersion7();
        var stamp = Guid.NewGuid().ToString("N");

        SelectOrganizationCommandHandler handler = CreateHandler(
            userId,
            stamp,
            organizationId,
            staffMembershipId,
            out RecordingSessionWriter sessions);

        LoginResult result = await handler.HandleAsync(
            new SelectOrganizationCommand("ticket", organizationId),
            CancellationToken.None);

        Assert.Equal(LoginStatus.Authenticated, result.Status);
        Assert.NotNull(sessions.LastSession);
        Assert.Equal(organizationId, sessions.LastSession!.ActiveOrganizationId);
        Assert.Equal(staffMembershipId, sessions.LastSession.StaffMembershipId);
    }

    private static SelectOrganizationCommandHandler CreateHandler(
        Guid userId,
        string stamp,
        Guid organizationId,
        Guid staffMembershipId,
        out RecordingSessionWriter sessions)
    {
        var identity = new AuthenticatedIdentity(
            userId,
            "user@test",
            "User",
            stamp,
            GlobalRoleId: null,
            IsActive: true);

        var users = new SelectOrgUserStore(identity);
        var memberships = new SelectOrgMembershipReader
        {
            Organizations =
            [
                new OrganizationOptionDto(organizationId, "Org A")
            ],
            StaffMembershipIdsByOrganization =
            {
                [organizationId] = staffMembershipId
            }
        };

        sessions = new RecordingSessionWriter();
        var tickets = new FixedTicketService(
            new OrganizationSelectionTicket(
                userId,
                stamp,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddMinutes(5),
                "oidc"));

        var accessor = new OrganizationContextAccessor();
        var profileBuilder = new AuthenticationProfileBuilder(
            accessor,
            new EmptyPermissionReader(),
            new EmptyPlatformPermissionReader());

        var sessionIssuer = new AuthenticationSessionIssuer(
            memberships,
            tickets,
            sessions,
            profileBuilder,
            new EmptyPlatformPermissionReader(),
            new NoopSecurityEvents(),
            new EmptyOrganizationContext(),
            TimeProvider.System);

        return new SelectOrganizationCommandHandler(
            tickets,
            users,
            memberships,
            sessionIssuer,
            TimeProvider.System);
    }

    private sealed class SelectOrgUserStore(AuthenticatedIdentity identity) : IUserAccountStore
    {
        public Task<AuthenticatedIdentity?> FindActiveByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedIdentity?>(
                identity.UserId == userId ? identity : null);

        public Task<ExternalIdentityMapping?> FindByExternalIdentityAsync(
            string provider,
            string subject,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ExternalIdentityMapping?>(null);

        public Task<bool> IsEmailInUseAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<ExternalUserProvisionResult> ProvisionAsync(
            ExternalUserProvisionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ExternalUserProvisionResult.EmailConflict());

        public Task RecordLoginAsync(
            Guid userIdentityId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class SelectOrgMembershipReader : IOrganizationMembershipReader
    {
        public IReadOnlyList<OrganizationOptionDto> Organizations { get; set; } = [];

        public Dictionary<Guid, Guid> StaffMembershipIdsByOrganization { get; } = new();

        public Task<IReadOnlyList<OrganizationOptionDto>> ListActiveOrganizationsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Organizations);

        public Task<bool> IsActiveMemberAsync(
            Guid userId,
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Organizations.Any(item => item.Id == organizationId));

        public Task<Guid?> GetActiveStaffMembershipIdAsync(
            Guid userId,
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                StaffMembershipIdsByOrganization.TryGetValue(organizationId, out Guid id)
                    ? id
                    : (Guid?)null);
    }

    private sealed class FixedTicketService(OrganizationSelectionTicket ticket)
        : IProtectedOrganizationSelectionTicketService
    {
        public string Protect(OrganizationSelectionTicket value) => "ticket";

        public OrganizationSelectionTicket? Unprotect(string protectedTicket) => ticket;
    }

    private sealed class RecordingSessionWriter : IAuthenticationSessionWriter
    {
        public AuthenticationSession? LastSession { get; private set; }

        public Task WriteAsync(
            AuthenticationSession session,
            CancellationToken cancellationToken = default)
        {
            LastSession = session;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyPermissionReader : IPermissionReader
    {
        public Task<bool> IsActiveMemberAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
    }

    private sealed class EmptyPlatformPermissionReader : IPlatformPermissionReader
    {
        public Task<bool> HasAnyPlatformRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));

        public Task<PlatformRoleSummary?> GetPrimaryRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformRoleSummary?>(null);
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
