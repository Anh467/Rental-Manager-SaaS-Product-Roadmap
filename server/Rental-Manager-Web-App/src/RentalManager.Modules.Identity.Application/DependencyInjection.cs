using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Authentication;
using RentalManager.Modules.Identity.Application.Authentication.CurrentUser;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Authentication.Logout;
using RentalManager.Modules.Identity.Application.Authentication.SelectOrganization;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.Identity.Application.PlatformUsers.Commands;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;
using RentalManager.Modules.Identity.Application.PlatformUsers.Queries;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

namespace RentalManager.Modules.Identity.Application;

public static class IdentityApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<AuthenticationProfileBuilder>();
        services.AddScoped<AuthenticationSessionIssuer>();

        services.AddScoped<
            ICommandHandler<ExternalLoginCommand, LoginResult>,
            ExternalLoginCommandHandler>();
        services.AddScoped<
            ICommandHandler<LogoutCommand>,
            LogoutCommandHandler>();
        services.AddScoped<
            ICommandHandler<SelectOrganizationCommand, LoginResult>,
            SelectOrganizationCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetCurrentUserQuery, CurrentUserDto>,
            GetCurrentUserQueryHandler>();

        services.AddScoped<
            IQueryHandler<GetPlatformUsersQuery, PagedResult<PlatformUserDto>>,
            GetPlatformUsersQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetPlatformUserQuery, PlatformUserDto>,
            GetPlatformUserQueryHandler>();
        services.AddScoped<
            ICommandHandler<UpdatePlatformUserCommand, PlatformUserDto>,
            UpdatePlatformUserCommandHandler>();
        services.AddScoped<
            ICommandHandler<ActivatePlatformUserCommand, PlatformUserDto>,
            ActivatePlatformUserCommandHandler>();
        services.AddScoped<
            ICommandHandler<InactivatePlatformUserCommand, PlatformUserDto>,
            InactivatePlatformUserCommandHandler>();

        return services;
    }
}
