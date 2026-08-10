using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Infrastructure.Sessions;

public sealed class CurrentIdentity(IHttpContextAccessor httpContextAccessor)
    : ICurrentIdentity
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? UserId => ReadGuid(IdentityClaimNames.UserId)
        ?? ReadGuid(ClaimTypes.NameIdentifier);

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public string? Scope =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(IdentityClaimNames.Scope);

    public Guid? ActiveOrganizationId =>
        ReadGuid(IdentityClaimNames.ActiveOrganizationId);

    public Guid? StaffMembershipId =>
        ReadGuid(IdentityClaimNames.StaffMembershipId);

    private Guid? ReadGuid(string claimType)
    {
        string? raw = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(raw, out Guid value) ? value : null;
    }
}
