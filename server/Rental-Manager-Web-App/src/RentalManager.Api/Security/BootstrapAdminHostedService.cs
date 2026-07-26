using Microsoft.Extensions.Options;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Api.Security;

/// <summary>
/// Create-only bootstrap for a local global administrator. Existing accounts are
/// never modified: password rotation and reactivation are separate workflows.
/// </summary>
public sealed class BootstrapAdminHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BootstrapAdminOptions> options,
    ILogger<BootstrapAdminHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        BootstrapAdminOptions value = options.Value;
        if (!value.Enabled)
        {
            return;
        }

        string email = value.Email!.Trim();

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        Role role = await roles.FindByKeyAsync("global_admin", cancellationToken)
            ?? throw new InvalidOperationException(
                "The global_admin role seed is required before bootstrap.");
        if (role.Scope != ERoleScope.Global)
        {
            throw new InvalidOperationException(
                "The configured global_admin role must have Global scope.");
        }

        User? user = await users.FindByNormalizedEmailAsync(
            email.ToUpperInvariant(),
            cancellationToken);

        if (user is null)
        {
            (string hash, string salt) = PasswordHasher.Create(value.Password!);
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
            logger.LogInformation("Bootstrap admin user created for {Email}.", email);
            return;
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                $"Bootstrap admin user '{email}' exists but is inactive. " +
                "Reactivation is not performed at startup.");
        }

        if (user.GlobalRoleId is null)
        {
            throw new InvalidOperationException(
                $"Bootstrap admin user '{email}' exists but has no global role. " +
                "Role assignment is not performed at startup.");
        }

        if (user.GlobalRoleId != role.Id)
        {
            throw new InvalidOperationException(
                $"User '{email}' already exists but is not assigned to the global_admin role.");
        }

        // Existing active global admin: create-only no-op. Password is never reset here.
        logger.LogInformation(
            "Bootstrap admin user {Email} already exists; skipping create.",
            email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
