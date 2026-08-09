using System.Security.Claims;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Api.Middlewares;

/// <summary>
/// Binds the organization context from the authenticated principal. This is the
/// only place the organization enters the request, and it is taken from the
/// cookie claims, never from the request body or query string.
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
            if (TryReadGuidClaim(principal, IdentityClaimNames.UserId, out Guid userId) ||
                TryReadGuidClaim(principal, ClaimTypes.NameIdentifier, out userId))
            {
                accessor.SetUser(userId);
            }

            bool hasOrganization = TryReadGuidClaim(
                principal,
                IdentityClaimNames.ActiveOrganizationId,
                out Guid organizationId);

            bool hasMembership = TryReadGuidClaim(
                principal,
                IdentityClaimNames.StaffMembershipId,
                out Guid staffMembershipId);

            if (hasOrganization)
            {
                // Organization-scoped sessions must carry a verified membership.
                if (!hasMembership)
                {
                    throw new MissingOrganizationContextException();
                }

                // A client may echo the organization back in a header for
                // logging, but it is only ever allowed to agree with the cookie.
                EnsureHeaderAgrees(context, organizationId);
                accessor.SetOrganization(organizationId, staffMembershipId);
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
