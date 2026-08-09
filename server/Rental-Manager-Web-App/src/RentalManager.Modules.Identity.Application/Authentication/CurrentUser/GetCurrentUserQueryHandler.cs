using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.Authentication.CurrentUser;

public sealed class GetCurrentUserQueryHandler(
    ICurrentIdentity currentIdentity,
    ICredentialAuthenticator credentialAuthenticator,
    AuthenticationProfileBuilder profileBuilder)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> HandleAsync(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!currentIdentity.IsAuthenticated ||
            currentIdentity.UserId is not Guid userId ||
            string.IsNullOrWhiteSpace(currentIdentity.Scope))
        {
            throw new AuthenticationFailedException();
        }

        AuthenticatedIdentity? identity =
            await credentialAuthenticator.FindByIdAsync(userId, cancellationToken);

        if (identity is null || !identity.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        AuthenticationResultDto profile = await profileBuilder.BuildAsync(
            identity,
            currentIdentity.Scope,
            currentIdentity.ActiveOrganizationId,
            cancellationToken);

        return profileBuilder.ToCurrentUser(profile);
    }
}
