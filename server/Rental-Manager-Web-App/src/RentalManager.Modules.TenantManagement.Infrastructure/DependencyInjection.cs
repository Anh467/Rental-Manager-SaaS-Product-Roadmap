using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Services;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Options;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Read.Connections;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Read.Stores;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Write.Connections;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Write.Repositories;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Write.Transactions;
using RentalManager.Modules.TenantManagement.Infrastructure.Services;

namespace RentalManager.Modules.TenantManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantManagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string writeConnectionString =
            configuration.GetConnectionString("RentalManagerWrite")
            ?? throw new InvalidOperationException(
                "Connection string 'RentalManagerWrite' is required.");

        string readConnectionString =
            configuration.GetConnectionString("RentalManagerRead")
            ?? writeConnectionString;

        var databaseOptions = new TenantManagementDatabaseOptions
        {
            ProviderInvariantName =
                configuration[$"{TenantManagementDatabaseOptions.SectionName}:ProviderInvariantName"]
                ?? "Microsoft.Data.SqlClient",
            WriteConnectionString = writeConnectionString,
            ReadConnectionString = readConnectionString
        };

        services.AddSingleton(databaseOptions);
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IWriteDbSession, SqlWriteDbSession>();
        services.AddScoped<IUnitOfWork, SqlUnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();

        services.AddScoped<IReadDbConnectionFactory, SqlReadDbConnectionFactory>();
        services.AddScoped<ITenantReadStore, TenantReadStore>();

        return services;
    }
}
