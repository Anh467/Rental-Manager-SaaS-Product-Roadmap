using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Authentication;

namespace RentalManager.Modules.Identity.Infrastructure.Sessions;

public sealed class CookieAuthenticationSessionWriter(
    IHttpContextAccessor httpContextAccessor)
    : IAuthenticationSessionWriter
{
    public async Task WriteAsync(
        AuthenticationSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        HttpContext httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "An HTTP context is required to write an authentication session.");

        var claims = new List<Claim>
        {
            new(IdentityClaimNames.UserId, session.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Email, session.Email),
            new(ClaimTypes.Name, session.DisplayName),
            new(IdentityClaimNames.Scope, session.Scope),

            // Federation is the only way a session is established, so the
            // authentication method is never a password value.
            new(
                ClaimTypes.AuthenticationMethod,
                IdentityClaimNames.AuthenticationMethodExternal)
        };

        if (!string.IsNullOrWhiteSpace(session.SecurityStamp))
        {
            claims.Add(new Claim(
                IdentityClaimNames.SecurityStamp,
                session.SecurityStamp));
        }

        if (!string.IsNullOrWhiteSpace(session.Provider))
        {
            claims.Add(new Claim(
                IdentityClaimNames.IdentityProvider,
                session.Provider));
        }

        if (session.ActiveOrganizationId is Guid organizationId)
        {
            claims.Add(new Claim(
                IdentityClaimNames.ActiveOrganizationId,
                organizationId.ToString()));
        }

        if (session.StaffMembershipId is Guid staffMembershipId)
        {
            claims.Add(new Claim(
                IdentityClaimNames.StaffMembershipId,
                staffMembershipId.ToString()));
        }

        var identity = new ClaimsIdentity(
            claims,
            IdentityAuthenticationSchemes.ApplicationCookie);

        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            IdentityAuthenticationSchemes.ApplicationCookie,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        // The external principal has served its purpose once the application
        // session exists; leaving it behind would keep a second, longer-lived
        // identity on the browser.
        await httpContext.SignOutAsync(IdentityAuthenticationSchemes.ExternalCookie);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        await httpContext.SignOutAsync(IdentityAuthenticationSchemes.ApplicationCookie);
        await httpContext.SignOutAsync(IdentityAuthenticationSchemes.ExternalCookie);
    }
}
