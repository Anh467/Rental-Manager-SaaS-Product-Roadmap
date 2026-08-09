using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.Identity.Infrastructure.Bootstrap;

/// <summary>
/// Create-only bootstrap of the first global administrator from a configured
/// external identity. Nothing about an existing account is ever modified, so a
/// misconfigured deployment can never silently escalate or reactivate a user.
/// </summary>
public sealed class BootstrapAdminHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BootstrapAdminOptions> options,
    IExternalAuthenticationPolicy externalAuthenticationPolicy,
    ILogger<BootstrapAdminHostedService> logger) : IHostedService
{
    private const string GlobalAdminRoleKey = "global_admin";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        BootstrapAdminOptions value = options.Value;
        if (!value.Enabled)
        {
            return;
        }

        string provider = value.Provider!.Trim();
        string subject = value.Subject!.Trim();
        string email = value.Email!.Trim();

        if (!externalAuthenticationPolicy.IsProviderAllowed(provider))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Provider must be listed in " +
                "Authentication:External:AllowedProviders.");
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();

        Role role = await roles.FindByKeyAsync(GlobalAdminRoleKey, cancellationToken)
            ?? throw new InvalidOperationException(
                "The global_admin role seed is required before bootstrap.");

        if (role.Scope != ERoleScope.Global)
        {
            throw new InvalidOperationException(
                "The configured global_admin role must have Global scope.");
        }

        ExternalIdentityMapping? existing =
            await users.FindByExternalIdentityAsync(provider, subject, cancellationToken);

        if (existing is not null)
        {
            EnsureUsableAdministrator(existing, role.Id);

            logger.LogInformation(
                "Bootstrap admin identity already exists for provider {Provider}; " +
                "skipping create.",
                provider);
            return;
        }

        ExternalUserProvisionResult result = await users.ProvisionAsync(
            new ExternalUserProvisionRequest(
                provider,
                subject,
                email,
                string.IsNullOrWhiteSpace(value.DisplayName)
                    ? email
                    : value.DisplayName.Trim(),
                role.Id),
            cancellationToken);

        switch (result.Status)
        {
            case ExternalUserProvisionStatus.Created:
                logger.LogInformation(
                    "Bootstrap admin user created for provider {Provider}.",
                    provider);
                return;

            case ExternalUserProvisionStatus.AlreadyMapped:
                EnsureUsableAdministrator(result.Mapping!, role.Id);
                return;

            default:
                throw new InvalidOperationException(
                    "The bootstrap admin email already belongs to another user. " +
                    "Accounts are never linked automatically; resolve the conflict " +
                    "before enabling bootstrap.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Startup refuses to continue rather than repairing the account, so an
    /// operator has to make the change deliberately.
    /// </summary>
    private static void EnsureUsableAdministrator(
        ExternalIdentityMapping mapping,
        Guid globalRoleId)
    {
        if (!mapping.User.IsActive)
        {
            throw new InvalidOperationException(
                "The bootstrap admin identity is mapped to an inactive user. " +
                "Reactivation is not performed at startup.");
        }

        if (mapping.User.GlobalRoleId is null)
        {
            throw new InvalidOperationException(
                "The bootstrap admin identity is mapped to a user without a global " +
                "role. Role assignment is not performed at startup.");
        }

        if (mapping.User.GlobalRoleId != globalRoleId)
        {
            throw new InvalidOperationException(
                "The bootstrap admin identity is mapped to a user that holds a " +
                "different global role.");
        }
    }
}
