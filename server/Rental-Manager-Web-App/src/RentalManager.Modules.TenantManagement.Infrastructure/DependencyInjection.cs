using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields.Commands;
using RentalManager.Modules.TenantManagement.Application.Fields.Queries;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;
using RentalManager.Modules.TenantManagement.Application.GlobalFieldTypes.Queries;
using RentalManager.Modules.TenantManagement.Infrastructure.Authorization;
using RentalManager.Modules.TenantManagement.Infrastructure.DependencyInjection;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

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
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'RentalManager' (or 'DefaultConnection') is required.");

        services.AddSingleton<ISqlConnectionFactory>(
            new SqlConnectionFactory(connectionString));

        // One session per scope owns the connection, the transaction and the row
        // level security context, so every repository in a use case shares them.
        services.AddScoped<SqlSession>();
        services.AddScoped<ISqlSession>(provider =>
            provider.GetRequiredService<SqlSession>());
        services.AddScoped<ISqlExecutionContext>(provider =>
            provider.GetRequiredService<SqlSession>());

        services.AddScoped<ISqlApplicationLock, SqlApplicationLock>();
        services.AddScoped<IPermissionReader, PermissionReader>();

        services.AddRepositories(
            contractAssembly: typeof(IFieldRepository).Assembly,
            implementationAssembly: typeof(TenantManagementServiceCollectionExtensions).Assembly);

        services.AddTenantManagementApplication();

        return services;
    }

    private static IServiceCollection AddTenantManagementApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IFieldQueryService, FieldQueryService>();
        services.AddScoped<IFieldCommandService, FieldCommandService>();

        services.AddScoped<IGlobalFieldQueryService, GlobalFieldQueryService>();
        services.AddScoped<IGlobalFieldCommandService, GlobalFieldCommandService>();

        services.AddScoped<IGlobalFieldTypeQueryService, GlobalFieldTypeQueryService>();

        return services;
    }
}
