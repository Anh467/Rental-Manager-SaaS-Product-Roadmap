using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Infrastructure.DependencyInjection;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

namespace RentalManager.Modules.TenantManagement.Infrastructure;

public static class TenantManagementServiceCollectionExtensions
{
    public static IServiceCollection AddTenantManagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString =
            configuration.GetConnectionString("RentalManager")
            ?? throw new InvalidOperationException(
                "Connection string 'RentalManager' is required.");

        services.AddSingleton<ISqlConnectionFactory>(
            new SqlConnectionFactory(connectionString));

        services.AddRepositories(
            contractAssembly: typeof(IFieldRepository).Assembly,
            implementationAssembly: typeof(TenantManagementServiceCollectionExtensions).Assembly);

        return services;
    }
}
