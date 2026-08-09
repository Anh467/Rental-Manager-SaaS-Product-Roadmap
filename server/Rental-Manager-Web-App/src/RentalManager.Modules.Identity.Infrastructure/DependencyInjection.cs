using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RentalManager.Modules.Identity.Application;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Authorization;
using RentalManager.Modules.Identity.Infrastructure.Bootstrap;
using RentalManager.Modules.Identity.Infrastructure.Identity;
using RentalManager.Modules.Identity.Infrastructure.Options;
using RentalManager.Modules.Identity.Infrastructure.Persistence;
using RentalManager.Modules.Identity.Infrastructure.Sessions;

namespace RentalManager.Modules.Identity.Infrastructure;

public static class IdentityInfrastructureServiceCollectionExtensions
{
    public const string AuthCookieNameProduction = "__Host-rentalmanager.auth";
    public const string AuthCookieNameDevelopment = "rentalmanager.auth";
    public const string AntiforgeryCookieNameProduction = "__Host-rentalmanager.af";
    public const string AntiforgeryCookieNameDevelopment = "rentalmanager.af";
    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";

    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        string connectionString =
            configuration.GetConnectionString("RentalManager")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'RentalManager' (or 'DefaultConnection') is required.");

        services.AddSingleton<IIdentityConnectionFactory>(
            new IdentityConnectionFactory(connectionString));

        services.AddIdentityApplication();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddUserStore<DapperUserStore>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IPasswordHasher<ApplicationUser>, LegacyCompatiblePasswordHasher>();

        string authCookieName = environment.IsDevelopment()
            ? AuthCookieNameDevelopment
            : AuthCookieNameProduction;

        string antiforgeryCookieName = environment.IsDevelopment()
            ? AntiforgeryCookieNameDevelopment
            : AntiforgeryCookieNameProduction;

        CookieSecurePolicy securePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = authCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = securePolicy;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryHeaderName;
            options.Cookie.Name = antiforgeryCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = securePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
        });

        IDataProtectionBuilder dataProtection = services
            .AddDataProtection()
            .SetApplicationName("RentalManager");

        string? keyRingPath = configuration["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }

        services
            .AddOptions<BootstrapAdminOptions>()
            .Bind(configuration.GetSection(BootstrapAdminOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<AuthRateLimitOptions>()
            .Bind(configuration.GetSection(AuthRateLimitOptions.SectionName));

        services.AddHostedService<BootstrapAdminHostedService>();

        services.AddScoped<ICredentialAuthenticator, IdentityCredentialAuthenticator>();
        services.AddScoped<IAuthenticationSessionWriter, CookieAuthenticationSessionWriter>();
        services.AddScoped<ICurrentIdentity, CurrentIdentity>();
        services.AddScoped<IOrganizationMembershipReader, OrganizationMembershipReader>();
        services.AddSingleton<
            IProtectedOrganizationSelectionTicketService,
            ProtectedOrganizationSelectionTicketService>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        return services;
    }
}
