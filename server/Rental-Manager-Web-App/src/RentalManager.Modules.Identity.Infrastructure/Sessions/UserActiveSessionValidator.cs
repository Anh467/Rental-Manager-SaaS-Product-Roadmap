using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Authentication;

namespace RentalManager.Modules.Identity.Infrastructure.Sessions;

/// <summary>
/// Rejects cookies for inactive users, after SecurityStamp rotation, or when an
/// organization-scoped session no longer has an active StaffMembership.
/// Failures sign the principal out without disclosing why.
/// </summary>
public sealed class UserActiveSessionValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        ClaimsPrincipal principal = context.Principal
            ?? throw new InvalidOperationException("Cookie principal is required.");

        Guid? userId = ReadGuid(principal, IdentityClaimNames.UserId)
            ?? ReadGuid(principal, ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            await RejectAsync(context, userId: null, SecurityEventReasons.SessionRejectedInactive);
            return;
        }

        string? stamp = principal.FindFirst(IdentityClaimNames.SecurityStamp)?.Value;

        IUserSessionStateReader sessions =
            context.HttpContext.RequestServices.GetRequiredService<IUserSessionStateReader>();

        UserSessionState? state = await sessions.GetAsync(
            userId.Value,
            context.HttpContext.RequestAborted);

        if (state is null ||
            state.DeletedAt is not null ||
            !state.IsActive ||
            (!string.IsNullOrWhiteSpace(stamp)
             && !string.Equals(stamp, state.SecurityStamp, StringComparison.Ordinal)))
        {
            string reason = state is null || state.DeletedAt is not null || !state.IsActive
                ? SecurityEventReasons.SessionRejectedInactive
                : SecurityEventReasons.SessionRejectedStampMismatch;

            await RejectAsync(context, userId, reason);
            return;
        }

        Guid? organizationId = ReadGuid(principal, IdentityClaimNames.ActiveOrganizationId);
        if (organizationId is Guid orgId)
        {
            IOrganizationMembershipReader memberships =
                context.HttpContext.RequestServices.GetRequiredService<IOrganizationMembershipReader>();

            Guid? staffMembershipId = await memberships.GetActiveStaffMembershipIdAsync(
                userId.Value,
                orgId,
                context.HttpContext.RequestAborted);

            Guid? claimMembershipId = ReadGuid(principal, IdentityClaimNames.StaffMembershipId);

            if (staffMembershipId is null ||
                claimMembershipId is null ||
                staffMembershipId.Value != claimMembershipId.Value)
            {
                await RejectAsync(
                    context,
                    userId,
                    SecurityEventReasons.SessionRejectedInactive);
            }
        }
    }

    private static async Task RejectAsync(
        CookieValidatePrincipalContext context,
        Guid? userId,
        string reason)
    {
        ISecurityEventPublisher? publisher =
            context.HttpContext.RequestServices.GetService<ISecurityEventPublisher>();

        if (publisher is not null)
        {
            await publisher.PublishAsync(
                SecurityEvent.Create(
                    SecurityEventTypes.InactiveUserRejected,
                    context.HttpContext.TraceIdentifier,
                    new Dictionary<string, object?>
                    {
                        [SecurityEventFields.UserId] = userId,
                        [SecurityEventFields.Reason] = reason,
                        [SecurityEventFields.Result] = SecurityEventReasons.Failed
                    }),
                context.HttpContext.RequestAborted);
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(
            IdentityAuthenticationSchemes.ApplicationCookie);
    }

    private static Guid? ReadGuid(ClaimsPrincipal principal, string claimType)
    {
        string? raw = principal.FindFirst(claimType)?.Value;
        return Guid.TryParse(raw, out Guid value) ? value : null;
    }
}
