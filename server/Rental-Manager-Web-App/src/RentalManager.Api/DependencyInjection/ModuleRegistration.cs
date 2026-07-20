using RentalManager.BuildingBlocks.Tenancy;
using RentalManager.Modules.TenantManagement.Application.Tenants.ActivateTenant;
using RentalManager.Modules.TenantManagement.Application.Tenants.CreateTenant;
using RentalManager.Modules.TenantManagement.Application.Tenants.GetTenantById;
using RentalManager.Modules.TenantManagement.Application.Tenants.SearchTenants;
using RentalManager.Modules.TenantManagement.Application.Tenants.SuspendTenant;
using RentalManager.Modules.TenantManagement.Application.Tenants.UpdateTenant;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Contracts.Tenants;
using RentalManager.Modules.TenantManagement.Infrastructure;

namespace RentalManager.Api.DependencyInjection;

public static class ModuleRegistration
{
    public static IServiceCollection AddApplicationModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();

        services.AddScoped<
            ICommandHandler<CreateTenantCommand, CreateTenantResult>,
            CreateTenantCommandHandler>();

        services.AddScoped<
            ICommandHandler<UpdateTenantCommand>,
            UpdateTenantCommandHandler>();

        services.AddScoped<
            ICommandHandler<ActivateTenantCommand>,
            ActivateTenantCommandHandler>();

        services.AddScoped<
            ICommandHandler<SuspendTenantCommand>,
            SuspendTenantCommandHandler>();

        services.AddScoped<
            IQueryHandler<GetTenantByIdQuery, TenantDetailsResponse?>,
            GetTenantByIdQueryHandler>();

        services.AddScoped<
            IQueryHandler<SearchTenantsQuery, PagedResult<TenantListItem>>,
            SearchTenantsQueryHandler>();

        services.AddTenantManagementInfrastructure(configuration);

        return services;
    }
}
