namespace RentalManager.Modules.Identity.Infrastructure.Options;

/// <summary>
/// Deployment configuration for external authentication. Everything here is
/// provider-agnostic: the concrete identity provider is described entirely by
/// <see cref="OpenIdConnectOptions"/> values supplied per environment, never by
/// a vendor name compiled into the application.
/// </summary>
public sealed class ExternalAuthenticationOptions
{
    public const string SectionName = "Authentication:External";

    public const string DefaultProviderKey = "oidc";

    /// <summary>
    /// Provider keys this deployment accepts. A key outside this list can never
    /// map or provision a user.
    /// </summary>
    public IList<string> AllowedProviders { get; set; } = [DefaultProviderKey];

    /// <summary>
    /// The authentication scheme the login challenge uses.
    /// </summary>
    public string DefaultScheme { get; set; } = DefaultProviderKey;

    /// <summary>
    /// The scheme the verified external principal is read back from after the
    /// provider handler has completed. Defaults to this application's external
    /// cookie; a test host points it at its own fake handler so no test ever
    /// talks to a real provider.
    /// </summary>
    public string PrincipalScheme { get; set; } =
        Authentication.IdentityAuthenticationSchemes.ExternalCookie;

    /// <summary>
    /// The stable provider key stored against every mapping. Defaults to
    /// <see cref="DefaultScheme"/> so a single-provider deployment needs no extra
    /// configuration, but it can be pinned separately so renaming a scheme never
    /// silently orphans existing mappings.
    /// </summary>
    public string? ProviderKey { get; set; }

    /// <summary>
    /// Whether an allow-listed provider may create a user on first login.
    /// </summary>
    public bool AllowFirstLoginProvisioning { get; set; } = true;

    /// <summary>
    /// Development-only escape hatch that lets the login endpoint accept an
    /// identity from the request body instead of a verified external principal,
    /// so the application can be run without a provider. Startup refuses to boot
    /// with this enabled outside Development.
    /// </summary>
    public bool AllowRequestBodyLogin { get; set; }

    /// <summary>
    /// Local path the browser is returned to after the provider redirects back.
    /// The SPA completes sign-in from there by calling the login endpoint.
    /// </summary>
    public string PostLoginRedirectPath { get; set; } = "/login/callback";

    public OpenIdConnectOptions Oidc { get; set; } = new();

    public string ResolvedProviderKey =>
        string.IsNullOrWhiteSpace(ProviderKey) ? DefaultScheme : ProviderKey;

    /// <summary>
    /// The provider handler is only registered once the deployment supplies an
    /// authority and a client id; until then the cookie session, the mapping use
    /// case and the development login path still work.
    /// </summary>
    public bool IsOpenIdConnectConfigured =>
        !string.IsNullOrWhiteSpace(Oidc.Authority) &&
        !string.IsNullOrWhiteSpace(Oidc.ClientId);

    public sealed class OpenIdConnectOptions
    {
        public string? Authority { get; set; }

        public string? ClientId { get; set; }

        /// <summary>
        /// Supplied through user secrets, environment variables or a secret
        /// store. Never committed to configuration files.
        /// </summary>
        public string? ClientSecret { get; set; }

        public string CallbackPath { get; set; } = "/signin-oidc";

        public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

        public bool RequireHttpsMetadata { get; set; } = true;

        public IList<string> Scopes { get; set; } = ["openid", "profile", "email"];
    }
}
