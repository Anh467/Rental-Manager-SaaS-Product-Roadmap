using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentalManager.Api.Security;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class BootstrapAdminHostedServiceTests
{
    private static readonly Guid GlobalAdminRoleId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Creates_global_admin_when_user_is_missing()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        BootstrapAdminHostedService service = CreateService(users, roles);

        await service.StartAsync(CancellationToken.None);

        User created = Assert.Single(users.Users);
        Assert.Equal("ops@example.com", created.Email);
        Assert.Equal(GlobalAdminRoleId, created.GlobalRoleId);
        Assert.True(created.IsActive);
        Assert.False(string.IsNullOrWhiteSpace(created.PasswordHash));
    }

    [Fact]
    public async Task Existing_active_global_admin_is_noop_and_does_not_reset_password()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        users.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Email = "ops@example.com",
            NormalizedEmail = "OPS@EXAMPLE.COM",
            DisplayName = "Ops",
            PasswordHash = "existing-hash",
            PasswordSalt = "existing-salt",
            GlobalRoleId = GlobalAdminRoleId,
            IsActive = true
        });

        BootstrapAdminHostedService service = CreateService(users, roles);
        await service.StartAsync(CancellationToken.None);

        User user = Assert.Single(users.Users);
        Assert.Equal("existing-hash", user.PasswordHash);
        Assert.Equal("existing-salt", user.PasswordSalt);
    }

    [Fact]
    public async Task Existing_inactive_user_fails_startup()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        users.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Email = "ops@example.com",
            NormalizedEmail = "OPS@EXAMPLE.COM",
            DisplayName = "Ops",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            GlobalRoleId = GlobalAdminRoleId,
            IsActive = false
        });

        BootstrapAdminHostedService service = CreateService(users, roles);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        Assert.Contains("inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("hash", users.Users[0].PasswordHash);
        Assert.False(users.Users[0].IsActive);
    }

    [Fact]
    public async Task Existing_user_without_global_role_fails_startup()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        users.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Email = "ops@example.com",
            NormalizedEmail = "OPS@EXAMPLE.COM",
            DisplayName = "Ops",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            GlobalRoleId = null,
            IsActive = true
        });

        BootstrapAdminHostedService service = CreateService(users, roles);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        Assert.Contains("no global role", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(users.Users[0].GlobalRoleId);
    }

    private static BootstrapAdminHostedService CreateService(
        FakeUserRepository users,
        FakeRoleRepository roles)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUserRepository>(users);
        services.AddSingleton<IRoleRepository>(roles);
        ServiceProvider provider = services.BuildServiceProvider();

        return new BootstrapAdminHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BootstrapAdminOptions
            {
                Enabled = true,
                Email = "ops@example.com",
                Password = "A-Strong-Local-Only-Password!"
            }),
            NullLogger<BootstrapAdminHostedService>.Instance);
    }

    private static Role CreateGlobalAdminRole() => new()
    {
        Id = GlobalAdminRoleId,
        Key = "global_admin",
        Name = "Global admin",
        Scope = ERoleScope.Global
    };

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> FindByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user =>
                string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal)));
        }

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(user => user.Id == id));

        public Task<IReadOnlyCollection<User>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<User>>(Users);

        public Task<byte[]?> InsertAsync(
            User entity,
            CancellationToken cancellationToken = default)
        {
            Users.Add(entity);
            return Task.FromResult<byte[]?>(null);
        }

        public Task<byte[]?> UpdateAsync(
            User entity,
            byte[]? expectedRowVersion = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            User entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            User entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SoftDeleteAsync(
            Guid id,
            byte[]? expectedRowVersion = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly Role _role;

        public FakeRoleRepository(Role role) => _role = role;

        public Task<Role?> FindByKeyAsync(
            string key,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                string.Equals(_role.Key, key, StringComparison.Ordinal) ? _role : null);

        public Task<Role?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_role.Id == id ? _role : null);

        public Task<IReadOnlyCollection<Role>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Role>>([_role]);

        public Task<byte[]?> InsertAsync(
            Role entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<byte[]?> UpdateAsync(
            Role entity,
            byte[]? expectedRowVersion = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            Role entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            Role entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SoftDeleteAsync(
            Guid id,
            byte[]? expectedRowVersion = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
