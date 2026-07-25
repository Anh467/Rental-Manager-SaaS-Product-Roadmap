using Microsoft.Extensions.DependencyInjection;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
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

        return services;
    }
}
