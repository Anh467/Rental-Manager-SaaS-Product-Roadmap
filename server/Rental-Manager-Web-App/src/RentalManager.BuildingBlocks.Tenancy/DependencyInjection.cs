using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Tenancy.Security;
using RentalManager.BuildingBlocks.Tenancy.Services;

namespace RentalManager.BuildingBlocks.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTenancy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<OrganizationContextAccessor>();
        services.AddScoped<IOrganizationContext>(provider =>
            provider.GetRequiredService<OrganizationContextAccessor>());

        services.AddSecurityEvents();

        return services;
    }

    /// <summary>
    /// Registers the default structured-log sink for security events. Uses
    /// <c>TryAdd</c> so a host that composes several modules keeps exactly one
    /// publisher, and so a durable sink registered earlier wins.
    /// </summary>
    public static IServiceCollection AddSecurityEvents(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ISecurityEventPublisher, LoggingSecurityEventPublisher>();

        return services;
    }
}
