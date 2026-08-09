using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Authentication;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using Xunit;
using DboRolePermissionRepository =
    RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo.IRolePermissionRepository;

namespace RentalManager.Modules.Identity.Application.UnitTests;

public sealed class ExternalLoginCommandHandlerTests
{
    private const string Provider = "oidc";
    private const string Subject = "subject-1";
    private const string Email = "user@example.com";

    [Fact]
    public async Task Rejects_provider_outside_allow_list()
    {
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            allowed: false,
            identity: new ExternalIdentityDescriptor("other", Subject, Email, "User"));

        ValidationFailedException exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => harness.Handler.HandleAsync(new ExternalLoginCommand("other", Subject, Email, "User")));

        Assert.Equal(MessageCode.Error.ValidationFailed, exception.MessageKey);
        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.LoginFailed
                && Equals(e.Data[SecurityEventFields.Reason], SecurityEventReasons.ProviderNotAllowed));
        Assert.DoesNotContain(harness.SecurityEvents.Events, ContainsSecret);
    }

    [Fact]
    public async Task Rejects_missing_subject()
    {
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            identity: new ExternalIdentityDescriptor(Provider, "  ", Email, "User"));

        ValidationFailedException exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => harness.Handler.HandleAsync(new ExternalLoginCommand(Provider, "  ", Email, "User")));

        Assert.Equal(MessageCode.Error.ValidationFailed, exception.MessageKey);
        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.LoginFailed
                && Equals(e.Data[SecurityEventFields.Reason], SecurityEventReasons.SubjectMissing));
    }

    [Fact]
    public async Task Maps_existing_external_identity()
    {
        AuthenticatedIdentity user = ActiveUser();
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            identity: new ExternalIdentityDescriptor(Provider, Subject, Email, "User"));
        harness.Users.Existing = new ExternalIdentityMapping(Guid.CreateVersion7(), Provider, user);
        harness.Memberships.Organizations =
        [
            new OrganizationOptionDto(Guid.CreateVersion7(), "Org A"),
            new OrganizationOptionDto(Guid.CreateVersion7(), "Org B")
        ];

        LoginResult result = await harness.Handler.HandleAsync(
            new ExternalLoginCommand(Provider, Subject, Email, "User"));

        Assert.Equal(LoginStatus.OrganizationSelectionRequired, result.Status);
        Assert.Equal(1, harness.Users.RecordLoginCalls);
        Assert.Empty(harness.Users.ProvisionCalls);
    }

    [Fact]
    public async Task Provisions_on_first_login()
    {
        AuthenticatedIdentity user = ActiveUser();
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            identity: new ExternalIdentityDescriptor(Provider, Subject, Email, "User"));
        harness.Users.ProvisionResult = ExternalUserProvisionResult.Created(
            new ExternalIdentityMapping(Guid.CreateVersion7(), Provider, user));
        harness.Memberships.Organizations =
        [
            new OrganizationOptionDto(Guid.CreateVersion7(), "Org A"),
            new OrganizationOptionDto(Guid.CreateVersion7(), "Org B")
        ];

        LoginResult result = await harness.Handler.HandleAsync(
            new ExternalLoginCommand(Provider, Subject, Email, "User"));

        Assert.Equal(LoginStatus.OrganizationSelectionRequired, result.Status);
        Assert.Single(harness.Users.ProvisionCalls);
        Assert.Null(harness.Users.ProvisionCalls[0].GlobalRoleId);
        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.UserProvisioned);
        Assert.DoesNotContain(harness.SecurityEvents.Events, ContainsSecret);
    }

    [Fact]
    public async Task Concurrent_provision_already_mapped_is_idempotent()
    {
        AuthenticatedIdentity user = ActiveUser();
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            identity: new ExternalIdentityDescriptor(Provider, Subject, Email, "User"));
        harness.Users.ProvisionResult = ExternalUserProvisionResult.AlreadyMapped(
            new ExternalIdentityMapping(Guid.CreateVersion7(), Provider, user));
        harness.Memberships.Organizations =
        [
            new OrganizationOptionDto(Guid.CreateVersion7(), "Org A"),
            new OrganizationOptionDto(Guid.CreateVersion7(), "Org B")
        ];

        LoginResult result = await harness.Handler.HandleAsync(
            new ExternalLoginCommand(Provider, Subject, Email, "User"));

        Assert.Equal(LoginStatus.OrganizationSelectionRequired, result.Status);
        Assert.DoesNotContain(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.UserProvisioned);
    }

    [Fact]
    public async Task Email_conflict_throws_ERR_041_and_publishes_identity_conflict()
    {
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            identity: new ExternalIdentityDescriptor(Provider, Subject, Email, "User"));
        harness.Users.EmailInUse = true;

        ExternalIdentityConflictException exception =
            await Assert.ThrowsAsync<ExternalIdentityConflictException>(
                () => harness.Handler.HandleAsync(
                    new ExternalLoginCommand(Provider, Subject, Email, "User")));

        Assert.Equal(MessageCode.Error.ExternalIdentityConflict, exception.MessageKey);
        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.IdentityConflict);
        Assert.DoesNotContain(harness.SecurityEvents.Events, ContainsSecret);
    }

    [Fact]
    public async Task Inactive_user_throws_ERR_009()
    {
        AuthenticatedIdentity user = ActiveUser() with { IsActive = false };
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            identity: new ExternalIdentityDescriptor(Provider, Subject, Email, "User"));
        harness.Users.Existing = new ExternalIdentityMapping(Guid.CreateVersion7(), Provider, user);

        InactiveResourceException exception = await Assert.ThrowsAsync<InactiveResourceException>(
            () => harness.Handler.HandleAsync(
                new ExternalLoginCommand(Provider, Subject, Email, "User")));

        Assert.Equal(MessageCode.Error.Inactive, exception.MessageKey);
        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.InactiveUserRejected);
        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.LoginFailed
                && Equals(e.Data[SecurityEventFields.Reason], SecurityEventReasons.UserInactive));
        Assert.DoesNotContain(harness.SecurityEvents.Events, ContainsSecret);
    }

    [Fact]
    public async Task Successful_login_publishes_security_event_without_secrets()
    {
        AuthenticatedIdentity user = ActiveUser();
        Guid organizationId = Guid.CreateVersion7();
        ExternalLoginHarness harness = ExternalLoginHarness.Create(
            identity: new ExternalIdentityDescriptor(Provider, Subject, Email, "User"));
        harness.Users.Existing = new ExternalIdentityMapping(Guid.CreateVersion7(), Provider, user);
        harness.Memberships.Organizations =
        [
            new OrganizationOptionDto(organizationId, "Org A")
        ];

        LoginResult result = await harness.Handler.HandleAsync(
            new ExternalLoginCommand(Provider, Subject, Email, "User"));

        Assert.Equal(LoginStatus.Authenticated, result.Status);
        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.LoginSucceeded);
        Assert.DoesNotContain(harness.SecurityEvents.Events, ContainsSecret);
        Assert.All(
            harness.SecurityEvents.Events.SelectMany(e => e.Data.Keys),
            key => Assert.DoesNotContain("password", key, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Missing_external_principal_fails_authentication()
    {
        ExternalLoginHarness harness = ExternalLoginHarness.Create(identity: null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => harness.Handler.HandleAsync(new ExternalLoginCommand(null, null, null, null)));

        Assert.Contains(
            harness.SecurityEvents.Events,
            e => e.EventType == SecurityEventTypes.LoginFailed
                && Equals(
                    e.Data[SecurityEventFields.Reason],
                    SecurityEventReasons.ExternalPrincipalMissing));
    }

    [Fact]
    public void Password_login_command_types_are_removed()
    {
        Assert.Null(
            typeof(ExternalLoginCommand).Assembly.GetType(
                "RentalManager.Modules.Identity.Application.Authentication.Login.LoginCommand"));
        Assert.Null(
            typeof(ExternalLoginCommand).Assembly.GetType(
                "RentalManager.Modules.Identity.Application.Abstractions.ICredentialAuthenticator"));
    }

    private static AuthenticatedIdentity ActiveUser() =>
        new(
            Guid.CreateVersion7(),
            Email,
            "User",
            Guid.NewGuid().ToString("N"),
            GlobalRoleId: null,
            IsActive: true);

    private static bool ContainsSecret(SecurityEvent securityEvent) =>
        securityEvent.Data.Values.Any(value =>
            value is string text
            && (text.Contains("password", StringComparison.OrdinalIgnoreCase)
                || text.Contains("secret", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)));
}

internal sealed class ExternalLoginHarness
{
    public required ExternalLoginCommandHandler Handler { get; init; }
    public required FakeUserAccountStore Users { get; init; }
    public required FakeOrganizationMembershipReader Memberships { get; init; }
    public required RecordingSecurityEventPublisher SecurityEvents { get; init; }

    public static ExternalLoginHarness Create(
        ExternalIdentityDescriptor? identity,
        bool allowed = true,
        bool allowFirstLogin = true)
    {
        var users = new FakeUserAccountStore();
        var memberships = new FakeOrganizationMembershipReader();
        var securityEvents = new RecordingSecurityEventPublisher();
        var sessions = new FakeAuthenticationSessionWriter();
        var tickets = new FakeSelectionTicketService();
        var organizationContext = new FakeOrganizationContext();
        var accessor = new OrganizationContextAccessor();
        var permissionReader = new FakePermissionReader();

        // Role repositories are unused on the organization-session path exercised
        // by these tests; the stubs only satisfy the constructor.
        var profileBuilder = new AuthenticationProfileBuilder(
            accessor,
            permissionReader,
            new EmptyPlatformPermissionReader(),
            new UnusedRoleRepository(),
            new UnusedRolePermissionRepository());

        var sessionIssuer = new AuthenticationSessionIssuer(
            memberships,
            tickets,
            sessions,
            profileBuilder,
            new EmptyPlatformPermissionReader(),
            securityEvents,
            organizationContext,
            TimeProvider.System);

        var handler = new ExternalLoginCommandHandler(
            new FakeExternalIdentityResolver(identity),
            users,
            new FakeExternalAuthenticationPolicy(allowed, allowFirstLogin),
            sessionIssuer,
            TimeProvider.System);

        return new ExternalLoginHarness
        {
            Handler = handler,
            Users = users,
            Memberships = memberships,
            SecurityEvents = securityEvents
        };
    }
}

internal sealed class FakeExternalIdentityResolver(ExternalIdentityDescriptor? identity)
    : IExternalIdentityResolver
{
    public Task<ExternalIdentityDescriptor?> ResolveAsync(
        ExternalIdentityDescriptor? requestSupplied,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(identity);
}

internal sealed class FakeExternalAuthenticationPolicy(bool allowed, bool allowFirstLogin)
    : IExternalAuthenticationPolicy
{
    public bool IsProviderAllowed(string provider) => allowed;

    public bool AllowsFirstLoginProvisioning(string provider) => allowed && allowFirstLogin;
}

internal sealed class FakeUserAccountStore : IUserAccountStore
{
    public ExternalIdentityMapping? Existing { get; set; }

    public bool EmailInUse { get; set; }

    public ExternalUserProvisionResult ProvisionResult { get; set; } =
        ExternalUserProvisionResult.EmailConflict();

    public List<ExternalUserProvisionRequest> ProvisionCalls { get; } = [];

    public int RecordLoginCalls { get; private set; }

    public Task<AuthenticatedIdentity?> FindActiveByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AuthenticatedIdentity?>(null);

    public Task<ExternalIdentityMapping?> FindByExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Existing);

    public Task<bool> IsEmailInUseAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EmailInUse);

    public Task<ExternalUserProvisionResult> ProvisionAsync(
        ExternalUserProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ProvisionCalls.Add(request);
        return Task.FromResult(ProvisionResult);
    }

    public Task RecordLoginAsync(
        Guid userIdentityId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        RecordLoginCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeOrganizationMembershipReader : IOrganizationMembershipReader
{
    public IReadOnlyList<OrganizationOptionDto> Organizations { get; set; } = [];

    public Task<IReadOnlyList<OrganizationOptionDto>> ListActiveOrganizationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Organizations);

    public Task<bool> IsActiveMemberAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Organizations.Any(item => item.Id == organizationId));
}

internal sealed class RecordingSecurityEventPublisher : ISecurityEventPublisher
{
    public List<SecurityEvent> Events { get; } = [];

    public Task PublishAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        Events.Add(securityEvent);
        return Task.CompletedTask;
    }
}

internal sealed class FakeAuthenticationSessionWriter : IAuthenticationSessionWriter
{
    public Task WriteAsync(
        AuthenticationSession session,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class FakeSelectionTicketService : IProtectedOrganizationSelectionTicketService
{
    public string Protect(OrganizationSelectionTicket ticket) => "ticket";

    public OrganizationSelectionTicket? Unprotect(string protectedTicket) => null;
}

internal sealed class FakeOrganizationContext : IOrganizationContext
{
    public Guid? OrganizationId => null;

    public Guid? UserId => null;

    public string? CorrelationId => "test-correlation";

    public bool HasOrganization => false;
}

internal sealed class EmptyPlatformPermissionReader : IPlatformPermissionReader
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

internal sealed class FakePermissionReader : IPermissionReader
{
    public Task<bool> IsActiveMemberAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
}

internal sealed class UnusedRoleRepository : IRoleRepository
{
    public Task<Role?> FindByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Role?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<byte[]?> InsertAsync(Role entity, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<byte[]?> UpdateAsync(
        Role entity,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task SaveAsync(Role entity, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DeleteAsync(Role entity, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task SoftDeleteAsync(
        Guid id,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyCollection<Role>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class UnusedRolePermissionRepository : DboRolePermissionRepository
{
    public Task<IReadOnlySet<string>> GetPermissionKeysByRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> ExistsAsync(
        RolePermission entity,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task SaveAsync(
        RolePermission entity,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DeleteAsync(
        RolePermission entity,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
