using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentalManager.Modules.Identity.Infrastructure.Identity;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.Identity.Infrastructure.Bootstrap;

/// <summary>
/// Create-only bootstrap for a local global administrator via UserManager.
/// Existing accounts are never modified.
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();

        Role role = await roles.FindByKeyAsync("global_admin", cancellationToken)
            ?? throw new InvalidOperationException(
                "The global_admin role seed is required before bootstrap.");

        if (role.Scope != ERoleScope.Global)
        {
            throw new InvalidOperationException(
                "The configured global_admin role must have Global scope.");
        }

        string normalizedEmail = userManager.NormalizeEmail(email)
            ?? email.ToUpperInvariant();

        ApplicationUser? user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = email,
                GlobalRoleId = role.Id,
                IsActive = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            IdentityResult createResult =
                await userManager.CreateAsync(user, value.Password!);

            if (!createResult.Succeeded)
            {
                string errors = string.Join(
                    "; ",
                    createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException(
                    $"Bootstrap admin user creation failed: {errors}");
            }

            logger.LogInformation("Bootstrap admin user created for {Email}.", email);
            return;
        }

        if (!user.IsActive || user.DeletedAt is not null)
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

        logger.LogInformation(
            "Bootstrap admin user {Email} already exists; skipping create.",
            email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
