using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using RentalManager.Modules.Identity.Application.Abstractions;

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
            new(ClaimTypes.AuthenticationMethod, "pwd")
        };

        if (!string.IsNullOrWhiteSpace(session.SecurityStamp))
        {
            claims.Add(new Claim(
                new ClaimsIdentityOptions().SecurityStampClaimType,
                session.SecurityStamp));
        }

        if (session.ActiveOrganizationId is Guid organizationId)
        {
            claims.Add(new Claim(
                IdentityClaimNames.ActiveOrganizationId,
                organizationId.ToString()));
        }

        var identity = new ClaimsIdentity(
            claims,
            IdentityConstants.ApplicationScheme);

        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }
}
