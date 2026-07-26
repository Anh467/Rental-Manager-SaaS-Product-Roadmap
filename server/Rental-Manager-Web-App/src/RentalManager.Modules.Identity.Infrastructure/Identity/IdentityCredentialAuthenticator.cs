using Microsoft.AspNetCore.Identity;
using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Infrastructure.Identity;

public sealed class IdentityCredentialAuthenticator(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : ICredentialAuthenticator
{
    public async Task<CredentialAuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        string normalizedEmail = userManager.NormalizeEmail(email.Trim())
            ?? email.Trim().ToUpperInvariant();

        ApplicationUser? user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null || !user.IsActive || user.DeletedAt is not null)
        {
            return CredentialAuthenticationResult.Failed();
        }

        SignInResult signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return CredentialAuthenticationResult.Failed();
        }

        // Re-load after possible rehash/lockout updates.
        ApplicationUser? refreshed = await userManager.FindByIdAsync(user.Id.ToString());
        if (refreshed is null || !refreshed.IsActive)
        {
            return CredentialAuthenticationResult.Failed();
        }

        return CredentialAuthenticationResult.Success(ToIdentity(refreshed));
    }

    public async Task<AuthenticatedIdentity?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive || user.DeletedAt is not null)
        {
            return null;
        }

        return ToIdentity(user);
    }

    private static AuthenticatedIdentity ToIdentity(ApplicationUser user) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.SecurityStamp ?? string.Empty,
            user.GlobalRoleId,
            user.IsActive);
}
