using Microsoft.Extensions.DependencyInjection;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Authentication;
using RentalManager.Modules.Identity.Application.Authentication.CurrentUser;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Authentication.Logout;
using RentalManager.Modules.Identity.Application.Authentication.SelectOrganization;
using RentalManager.Modules.Identity.Application.Contracts;

namespace RentalManager.Modules.Identity.Application;

public static class IdentityApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<AuthenticationProfileBuilder>();

        services.AddScoped<
            ICommandHandler<LoginCommand, LoginResult>,
            LoginCommandHandler>();
        services.AddScoped<
            ICommandHandler<LogoutCommand>,
            LogoutCommandHandler>();
        services.AddScoped<
            ICommandHandler<SelectOrganizationCommand, LoginResult>,
            SelectOrganizationCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetCurrentUserQuery, CurrentUserDto>,
            GetCurrentUserQueryHandler>();

        return services;
    }
}
