using System.Security.Claims;
using RentalManager.Api.Security;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Api.Middlewares;

/// <summary>
/// Binds the organization context from the authenticated principal. This is the
/// only place the organization enters the request, and it is taken from the
/// token, never from the request body or query string.
/// </summary>
public sealed class OrganizationContextMiddleware
{
    public const string OrganizationHeaderName = "X-Organization-Id";

    private readonly RequestDelegate _next;

    public OrganizationContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        OrganizationContextAccessor accessor)
    {
        accessor.SetCorrelationId(context.TraceIdentifier);

        ClaimsPrincipal principal = context.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            if (TryReadGuidClaim(principal, JwtClaimNames.Subject, out Guid userId))
            {
                accessor.SetUser(userId);
            }

            if (TryReadGuidClaim(
                    principal,
                    JwtClaimNames.OrganizationId,
                    out Guid organizationId))
            {
                // A client may echo the organization back in a header for
                // logging, but it is only ever allowed to agree with the token.
                EnsureHeaderAgrees(context, organizationId);
                accessor.SetOrganization(organizationId);
            }
        }

        await _next(context);
    }

    private static void EnsureHeaderAgrees(HttpContext context, Guid organizationId)
    {
        if (!context.Request.Headers.TryGetValue(
                OrganizationHeaderName,
                out Microsoft.Extensions.Primitives.StringValues headerValues))
        {
            return;
        }

        string? headerValue = headerValues.ToString();

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return;
        }

        if (!Guid.TryParse(headerValue, out Guid headerOrganizationId) ||
            headerOrganizationId != organizationId)
        {
            throw new MissingOrganizationContextException();
        }
    }

    private static bool TryReadGuidClaim(
        ClaimsPrincipal principal,
        string claimType,
        out Guid value)
    {
        string? raw = principal.FindFirstValue(claimType);

        return Guid.TryParse(raw, out value);
    }
}
