using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Bootstrap;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class BootstrapAdminHostedServiceTests
{
    private static readonly Guid GlobalAdminRoleId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private const string Provider = "oidc";
    private const string Subject = "bootstrap-admin-subject";
    private const string Email = "ops@example.com";

    [Fact]
    public async Task Creates_global_admin_when_identity_is_missing()
    {
        var store = new FakeUserAccountStore();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        BootstrapAdminHostedService service = CreateService(store, roles);

        await service.StartAsync(CancellationToken.None);

        ExternalUserProvisionRequest request = Assert.Single(store.ProvisionCalls);
        Assert.Equal(Provider, request.Provider);
        Assert.Equal(Subject, request.Subject);
        Assert.Equal(Email, request.Email);
        Assert.Equal(GlobalAdminRoleId, request.GlobalRoleId);
        Assert.Null(store.LastPasswordHash);
    }

    [Fact]
    public async Task Existing_active_global_admin_is_noop()
    {
        var store = new FakeUserAccountStore
        {
            Existing = new ExternalIdentityMapping(
                Guid.CreateVersion7(),
                Provider,
                new AuthenticatedIdentity(
                    Guid.CreateVersion7(),
                    Email,
                    "Ops",
                    Guid.NewGuid().ToString("N"),
                    GlobalAdminRoleId,
                    IsActive: true))
        };
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        BootstrapAdminHostedService service = CreateService(store, roles);

        await service.StartAsync(CancellationToken.None);

        Assert.Empty(store.ProvisionCalls);
    }

    [Fact]
    public async Task Existing_inactive_user_fails_startup()
    {
        var store = new FakeUserAccountStore
        {
            Existing = new ExternalIdentityMapping(
                Guid.CreateVersion7(),
                Provider,
                new AuthenticatedIdentity(
                    Guid.CreateVersion7(),
                    Email,
                    "Ops",
                    Guid.NewGuid().ToString("N"),
                    GlobalAdminRoleId,
                    IsActive: false))
        };
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        BootstrapAdminHostedService service = CreateService(store, roles);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        Assert.Contains("inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.ProvisionCalls);
    }

    [Fact]
    public async Task Existing_user_without_global_role_fails_startup()
    {
        var store = new FakeUserAccountStore
        {
            Existing = new ExternalIdentityMapping(
                Guid.CreateVersion7(),
                Provider,
                new AuthenticatedIdentity(
                    Guid.CreateVersion7(),
                    Email,
                    "Ops",
                    Guid.NewGuid().ToString("N"),
                    GlobalRoleId: null,
                    IsActive: true))
        };
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        BootstrapAdminHostedService service = CreateService(store, roles);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        Assert.Contains("without a global", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.ProvisionCalls);
    }

    private static BootstrapAdminHostedService CreateService(
        FakeUserAccountStore store,
        FakeRoleRepository roles)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IRoleRepository>(roles);
        services.AddSingleton<IUserAccountStore>(store);

        ServiceProvider provider = services.BuildServiceProvider();

        return new BootstrapAdminHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BootstrapAdminOptions
            {
                Enabled = true,
                Provider = Provider,
                Subject = Subject,
                Email = Email,
                DisplayName = "Ops"
            }),
            new AllowAllExternalAuthenticationPolicy(),
            NullLogger<BootstrapAdminHostedService>.Instance);
    }

    private static Role CreateGlobalAdminRole() => new()
    {
        Id = GlobalAdminRoleId,
        Key = "global_admin",
        Name = "Global admin",
        Scope = ERoleScope.Global
    };

    private sealed class AllowAllExternalAuthenticationPolicy : IExternalAuthenticationPolicy
    {
        public bool IsProviderAllowed(string provider) => true;

        public bool AllowsFirstLoginProvisioning(string provider) => true;
    }

    private sealed class FakeUserAccountStore : IUserAccountStore
    {
        public ExternalIdentityMapping? Existing { get; set; }

        public List<ExternalUserProvisionRequest> ProvisionCalls { get; } = [];

        public string? LastPasswordHash { get; private set; }

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
            Task.FromResult(false);

        public Task<ExternalUserProvisionResult> ProvisionAsync(
            ExternalUserProvisionRequest request,
            CancellationToken cancellationToken = default)
        {
            ProvisionCalls.Add(request);
            LastPasswordHash = null;
            var mapping = new ExternalIdentityMapping(
                Guid.CreateVersion7(),
                request.Provider,
                new AuthenticatedIdentity(
                    Guid.CreateVersion7(),
                    request.Email,
                    request.DisplayName,
                    Guid.NewGuid().ToString("N"),
                    request.GlobalRoleId,
                    IsActive: true));
            Existing = mapping;
            return Task.FromResult(ExternalUserProvisionResult.Created(mapping));
        }

        public Task RecordLoginAsync(
            Guid userIdentityId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeRoleRepository(Role role) : IRoleRepository
    {
        public Task<Role?> FindByKeyAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(key == role.Key ? role : null);

        public Task<Role?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == role.Id ? role : null);

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
            Task.FromResult<IReadOnlyCollection<Role>>([role]);
    }
}
