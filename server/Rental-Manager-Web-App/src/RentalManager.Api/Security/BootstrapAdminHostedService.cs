using Microsoft.Extensions.Options;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Api.Security;

public sealed class BootstrapAdminHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BootstrapAdminOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        BootstrapAdminOptions value = options.Value;
        if (!value.Enabled) return;
        if (string.IsNullOrWhiteSpace(value.Email) || string.IsNullOrWhiteSpace(value.Password))
            throw new InvalidOperationException("BootstrapAdmin requires Email and Password when enabled.");

        using IServiceScope scope = scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        Role role = await roles.FindByKeyAsync("global_admin", cancellationToken)
            ?? throw new InvalidOperationException("The global_admin role seed is required before bootstrap.");
        if (role.Scope != ERoleScope.Global)
            throw new InvalidOperationException("The configured global_admin role must have Global scope.");

        string email = value.Email.Trim();
        User? user = await users.FindByNormalizedEmailAsync(email.ToUpperInvariant(), cancellationToken);
        if (user is null)
        {
            (string hash, string salt) = PasswordHasher.Create(value.Password);
            user = new User
            {
                Id = Guid.CreateVersion7(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                DisplayName = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                GlobalRoleId = role.Id,
                IsActive = true
            };
            await users.InsertAsync(user, cancellationToken);
            return;
        }

        // Resume a prior partial bootstrap that created the account without a role.
        if (user.GlobalRoleId is null)
        {
            user.GlobalRoleId = role.Id;
            await users.UpdateAsync(user, cancellationToken: cancellationToken);
            return;
        }

        if (user.GlobalRoleId != role.Id)
        {
            throw new InvalidOperationException(
                $"User '{email}' already exists but is not assigned to the global_admin role.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
