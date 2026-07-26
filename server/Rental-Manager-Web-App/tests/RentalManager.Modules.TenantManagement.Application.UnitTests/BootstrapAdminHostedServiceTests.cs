using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentalManager.Modules.Identity.Infrastructure.Bootstrap;
using RentalManager.Modules.Identity.Infrastructure.Identity;
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
        var store = new FakeUserStore();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        BootstrapAdminHostedService service = CreateService(store, roles);

        await service.StartAsync(CancellationToken.None);

        ApplicationUser created = Assert.Single(store.Users);
        Assert.Equal("ops@example.com", created.Email);
        Assert.Equal(GlobalAdminRoleId, created.GlobalRoleId);
        Assert.True(created.IsActive);
        Assert.False(string.IsNullOrWhiteSpace(created.PasswordHash));
    }

    [Fact]
    public async Task Existing_active_global_admin_is_noop_and_does_not_reset_password()
    {
        var store = new FakeUserStore();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        store.Users.Add(new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = "ops@example.com",
            NormalizedUserName = "OPS@EXAMPLE.COM",
            Email = "ops@example.com",
            NormalizedEmail = "OPS@EXAMPLE.COM",
            DisplayName = "Ops",
            PasswordHash = "existing-hash",
            PasswordSalt = "existing-salt",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            GlobalRoleId = GlobalAdminRoleId,
            IsActive = true
        });

        BootstrapAdminHostedService service = CreateService(store, roles);
        await service.StartAsync(CancellationToken.None);

        ApplicationUser user = Assert.Single(store.Users);
        Assert.Equal("existing-hash", user.PasswordHash);
        Assert.Equal("existing-salt", user.PasswordSalt);
    }

    [Fact]
    public async Task Existing_inactive_user_fails_startup()
    {
        var store = new FakeUserStore();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        store.Users.Add(new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = "ops@example.com",
            NormalizedUserName = "OPS@EXAMPLE.COM",
            Email = "ops@example.com",
            NormalizedEmail = "OPS@EXAMPLE.COM",
            DisplayName = "Ops",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            GlobalRoleId = GlobalAdminRoleId,
            IsActive = false
        });

        BootstrapAdminHostedService service = CreateService(store, roles);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        Assert.Contains("inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("hash", store.Users[0].PasswordHash);
        Assert.False(store.Users[0].IsActive);
    }

    [Fact]
    public async Task Existing_user_without_global_role_fails_startup()
    {
        var store = new FakeUserStore();
        var roles = new FakeRoleRepository(CreateGlobalAdminRole());
        store.Users.Add(new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = "ops@example.com",
            NormalizedUserName = "OPS@EXAMPLE.COM",
            Email = "ops@example.com",
            NormalizedEmail = "OPS@EXAMPLE.COM",
            DisplayName = "Ops",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            GlobalRoleId = null,
            IsActive = true
        });

        BootstrapAdminHostedService service = CreateService(store, roles);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        Assert.Contains("no global role", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(store.Users[0].GlobalRoleId);
    }

    private static BootstrapAdminHostedService CreateService(
        FakeUserStore store,
        FakeRoleRepository roles)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IRoleRepository>(roles);
        services.AddSingleton(store);
        services.AddSingleton<IUserStore<ApplicationUser>>(store);
        services.AddSingleton<IUserPasswordStore<ApplicationUser>>(store);
        services.AddSingleton<IUserEmailStore<ApplicationUser>>(store);
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            });

        ServiceProvider provider = services.BuildServiceProvider();

        return new BootstrapAdminHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BootstrapAdminOptions
            {
                Enabled = true,
                Email = "ops@example.com",
                Password = "A-Strong-Local-Only-Password1!"
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

    private sealed class FakeUserStore :
        IUserStore<ApplicationUser>,
        IUserPasswordStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>
    {
        public List<ApplicationUser> Users { get; } = [];

        public void Dispose()
        {
        }

        public Task<IdentityResult> CreateAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationUser?> FindByIdAsync(
            string userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(user => user.Id.ToString() == userId));

        public Task<ApplicationUser?> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(user =>
                string.Equals(
                    user.NormalizedUserName,
                    normalizedUserName,
                    StringComparison.Ordinal)));

        public Task<string?> GetNormalizedUserNameAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task<string> GetUserIdAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetNormalizedUserNameAsync(
            ApplicationUser user,
            string? normalizedName,
            CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(
            ApplicationUser user,
            string? userName,
            CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task SetPasswordHashAsync(
            ApplicationUser user,
            string? passwordHash,
            CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash);

        public Task<bool> HasPasswordAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

        public Task SetEmailAsync(
            ApplicationUser user,
            string? email,
            CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.EmailConfirmed);

        public Task SetEmailConfirmedAsync(
            ApplicationUser user,
            bool confirmed,
            CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task<ApplicationUser?> FindByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(user =>
                string.Equals(
                    user.NormalizedEmail,
                    normalizedEmail,
                    StringComparison.Ordinal)));

        public Task<string?> GetNormalizedEmailAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedEmail);

        public Task SetNormalizedEmailAsync(
            ApplicationUser user,
            string? normalizedEmail,
            CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }
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
