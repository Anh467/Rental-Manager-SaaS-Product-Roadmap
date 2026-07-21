using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Infrastructure.DependencyInjection;

namespace RentalManager.Modules.TenantManagement.Infrastructure;

public static class TenantManagementServiceCollectionExtensions
{
    public static IServiceCollection AddTenantManagementInfrastructure(
        this IServiceCollection services)
    {
        services.AddRepositories(
            contractAssembly: typeof(IFieldRepository).Assembly,
            implementationAssembly: typeof(TenantManagementServiceCollectionExtensions).Assembly);

        return services;
    }
}
