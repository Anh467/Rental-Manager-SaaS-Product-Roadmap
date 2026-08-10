using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Options;

namespace RentalManager.Modules.Identity.Infrastructure.Authentication;

/// <summary>
/// Reads the identity from the external authentication scheme's principal. The
/// subject comes from the provider's <c>sub</c> claim as the middleware verified
/// it; nothing the browser can set is trusted.
/// </summary>
public sealed class HttpExternalIdentityResolver(
    IHttpContextAccessor httpContextAccessor,
    IAuthenticationSchemeProvider schemeProvider,
    IOptionsMonitor<ExternalAuthenticationOptions> options)
    : IExternalIdentityResolver
{
    private const string SubjectClaim = "sub";

    public async Task<ExternalIdentityDescriptor?> ResolveAsync(
        ExternalIdentityDescriptor? requestSupplied,
        CancellationToken cancellationToken = default)
    {
        ExternalAuthenticationOptions current = options.CurrentValue;
        HttpContext? httpContext = httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            ExternalIdentityDescriptor? verified =
                await ReadVerifiedPrincipalAsync(httpContext, current);

            if (verified is not null)
            {
                return verified;
            }
        }

        // A malformed payload is left for the use case to validate so the caller
        // gets a field error rather than an unexplained challenge.
        if (!current.AllowRequestBodyLogin || requestSupplied is null)
        {
            return null;
        }

        return requestSupplied with
        {
            Provider = string.IsNullOrWhiteSpace(requestSupplied.Provider)
                ? current.ResolvedProviderKey
                : requestSupplied.Provider
        };
    }

    /// <summary>
    /// The provider handler is not registered until the deployment configures an
    /// authority, so the scheme is probed rather than assumed.
    /// </summary>
    private async Task<ExternalIdentityDescriptor?> ReadVerifiedPrincipalAsync(
        HttpContext httpContext,
        ExternalAuthenticationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PrincipalScheme) ||
            await schemeProvider.GetSchemeAsync(options.PrincipalScheme) is null)
        {
            return null;
        }

        AuthenticateResult result =
            await httpContext.AuthenticateAsync(options.PrincipalScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            return null;
        }

        string? subject = result.Principal.FindFirstValue(SubjectClaim)
            ?? result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        return new ExternalIdentityDescriptor(
            options.ResolvedProviderKey,
            subject,
            result.Principal.FindFirstValue(ClaimTypes.Email)
                ?? result.Principal.FindFirstValue("email"),
            result.Principal.FindFirstValue("name")
                ?? result.Principal.FindFirstValue(ClaimTypes.Name));
    }
}
