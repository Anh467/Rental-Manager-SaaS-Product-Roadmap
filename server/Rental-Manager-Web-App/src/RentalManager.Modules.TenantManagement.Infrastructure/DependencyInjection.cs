using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields.Commands;
using RentalManager.Modules.TenantManagement.Application.Fields.Queries;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;
using RentalManager.Modules.TenantManagement.Application.GlobalFieldTypes.Queries;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
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
        services.AddScoped<OrgFieldOptionSynchronizer>();
        services.AddScoped<GlobalFieldOptionSynchronizer>();

        services.AddScoped<
            IQueryHandler<GetFieldsQuery, PagedResult<FieldDto>>,
            GetFieldsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetFieldQuery, FieldDto>,
            GetFieldQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateFieldCommand, FieldDto>,
            CreateFieldCommandHandler>();
        services.AddScoped<
            ICommandHandler<UpdateFieldCommand, FieldDto>,
            UpdateFieldCommandHandler>();
        services.AddScoped<
            ICommandHandler<DeleteFieldCommand>,
            DeleteFieldCommandHandler>();

        services.AddScoped<
            IQueryHandler<GetGlobalFieldsQuery, PagedResult<FieldDto>>,
            GetGlobalFieldsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetGlobalFieldQuery, FieldDto>,
            GetGlobalFieldQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateGlobalFieldCommand, FieldDto>,
            CreateGlobalFieldCommandHandler>();
        services.AddScoped<
            ICommandHandler<UpdateGlobalFieldCommand, FieldDto>,
            UpdateGlobalFieldCommandHandler>();
        services.AddScoped<
            ICommandHandler<UpdateGlobalFieldStatusCommand, FieldDto>,
            UpdateGlobalFieldStatusCommandHandler>();
        services.AddScoped<
            ICommandHandler<DeleteGlobalFieldCommand>,
            DeleteGlobalFieldCommandHandler>();

        services.AddScoped<
            IQueryHandler<GetGlobalFieldTypesQuery, IReadOnlyList<FieldTypeDto>>,
            GetGlobalFieldTypesQueryHandler>();

        return services;
    }
}
