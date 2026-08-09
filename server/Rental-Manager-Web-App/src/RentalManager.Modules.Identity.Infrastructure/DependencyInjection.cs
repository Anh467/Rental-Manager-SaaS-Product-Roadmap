using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RentalManager.Modules.Identity.Application;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Authentication;
using RentalManager.Modules.Identity.Infrastructure.Authorization;
using RentalManager.Modules.Identity.Infrastructure.Bootstrap;
using RentalManager.Modules.Identity.Infrastructure.Options;
using RentalManager.Modules.Identity.Infrastructure.Persistence;
using RentalManager.Modules.Identity.Infrastructure.Sessions;

namespace RentalManager.Modules.Identity.Infrastructure;

public static class IdentityInfrastructureServiceCollectionExtensions
{
    public const string AuthCookieNameProduction = "__Host-rentalmanager.auth";
    public const string AuthCookieNameDevelopment = "rentalmanager.auth";
    public const string ExternalCookieNameProduction = "__Host-rentalmanager.ext";
    public const string ExternalCookieNameDevelopment = "rentalmanager.ext";
    public const string AntiforgeryCookieNameProduction = "__Host-rentalmanager.af";
    public const string AntiforgeryCookieNameDevelopment = "rentalmanager.af";
    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";

    /// <summary>
    /// The external principal only has to survive the redirect back from the
    /// provider and one call from the SPA.
    /// </summary>
    private static readonly TimeSpan ExternalCookieLifetime = TimeSpan.FromMinutes(10);

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
            .AddOptions<ExternalAuthenticationOptions>()
            .Bind(configuration.GetSection(ExternalAuthenticationOptions.SectionName))
            .Validate(
                options => !options.AllowRequestBodyLogin || environment.IsDevelopment(),
                "Authentication:External:AllowRequestBodyLogin may only be enabled in " +
                "Development. Outside Development an identity must come from the " +
                "configured provider.")
            .Validate(
                options => options.AllowedProviders.Count > 0,
                "Authentication:External:AllowedProviders must list at least one provider.")
            .ValidateOnStart();

        ExternalAuthenticationOptions externalAuthentication =
            ReadExternalAuthenticationOptions(configuration);

        string authCookieName = environment.IsDevelopment()
            ? AuthCookieNameDevelopment
            : AuthCookieNameProduction;

        string externalCookieName = environment.IsDevelopment()
            ? ExternalCookieNameDevelopment
            : ExternalCookieNameProduction;

        string antiforgeryCookieName = environment.IsDevelopment()
            ? AntiforgeryCookieNameDevelopment
            : AntiforgeryCookieNameProduction;

        CookieSecurePolicy securePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        AuthenticationBuilder authentication = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    IdentityAuthenticationSchemes.ApplicationCookie;
                options.DefaultChallengeScheme =
                    IdentityAuthenticationSchemes.ApplicationCookie;
                options.DefaultSignInScheme =
                    IdentityAuthenticationSchemes.ApplicationCookie;
            })
            .AddCookie(IdentityAuthenticationSchemes.ApplicationCookie, options =>
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
            })
            .AddCookie(IdentityAuthenticationSchemes.ExternalCookie, options =>
            {
                options.Cookie.Name = externalCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = securePolicy;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = ExternalCookieLifetime;
                options.SlidingExpiration = false;
            });

        AddOpenIdConnectProvider(authentication, externalAuthentication);

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

        services.AddScoped<IUserAccountStore, SqlUserAccountStore>();
        services.AddScoped<IExternalIdentityResolver, HttpExternalIdentityResolver>();
        services.AddSingleton<IExternalAuthenticationPolicy, ExternalAuthenticationPolicy>();
        services.AddSingleton<IExternalLoginChallengeFactory, ExternalLoginChallengeFactory>();
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

    /// <summary>
    /// The provider handler is registered only when the deployment supplies an
    /// authority and client id. Until then the application still boots and the
    /// cookie session, the mapping use case and the Development login path all
    /// work, which keeps a first-time checkout runnable without credentials.
    /// </summary>
    private static void AddOpenIdConnectProvider(
        AuthenticationBuilder authentication,
        ExternalAuthenticationOptions externalAuthentication)
    {
        if (!externalAuthentication.IsOpenIdConnectConfigured)
        {
            return;
        }

        ExternalAuthenticationOptions.OpenIdConnectOptions oidc = externalAuthentication.Oidc;

        authentication.AddOpenIdConnect(externalAuthentication.DefaultScheme, options =>
        {
            options.Authority = oidc.Authority;
            options.ClientId = oidc.ClientId;
            options.ClientSecret = oidc.ClientSecret;
            options.RequireHttpsMetadata = oidc.RequireHttpsMetadata;
            options.CallbackPath = oidc.CallbackPath;
            options.SignedOutCallbackPath = oidc.SignedOutCallbackPath;

            // The verified principal lands in the short-lived external cookie;
            // TicketReceived maps it onto a user and issues the application
            // session when organization selection is not required.
            options.SignInScheme = IdentityAuthenticationSchemes.ExternalCookie;

            options.ResponseType = "code";
            options.UsePkce = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = false;
            options.MapInboundClaims = false;

            options.Scope.Clear();
            foreach (string scope in oidc.Scopes)
            {
                if (!string.IsNullOrWhiteSpace(scope))
                {
                    options.Scope.Add(scope);
                }
            }

            options.TokenValidationParameters.NameClaimType = "name";
            options.Events = new OpenIdConnectLoginEvents(externalAuthentication);
        });
    }

    /// <summary>
    /// Whether to register the provider handler is a startup-time decision, so
    /// the section is read directly here rather than through the options monitor
    /// that request-time consumers use.
    /// </summary>
    private static ExternalAuthenticationOptions ReadExternalAuthenticationOptions(
        IConfiguration configuration)
    {
        var options = new ExternalAuthenticationOptions();
        configuration
            .GetSection(ExternalAuthenticationOptions.SectionName)
            .Bind(options);

        return options;
    }
}
