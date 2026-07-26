using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.Authentication.SelectOrganization;

public sealed class SelectOrganizationCommandHandler(
    IProtectedOrganizationSelectionTicketService selectionTicketService,
    ICredentialAuthenticator credentialAuthenticator,
    IOrganizationMembershipReader organizationMembershipReader,
    IAuthenticationSessionWriter sessionWriter,
    AuthenticationProfileBuilder profileBuilder)
    : ICommandHandler<SelectOrganizationCommand, LoginResult>
{
    public async Task<LoginResult> HandleAsync(
        SelectOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.SelectionTicket) ||
            command.OrganizationId is not Guid organizationId ||
            organizationId == Guid.Empty)
        {
            throw new ValidationFailedException(
                nameof(command.OrganizationId),
                MessageCode.Error.ValidationFailed);
        }

        OrganizationSelectionTicket? ticket =
            selectionTicketService.Unprotect(command.SelectionTicket);

        if (ticket is null || ticket.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new AuthenticationFailedException();
        }

        AuthenticatedIdentity? identity =
            await credentialAuthenticator.FindByIdAsync(
                ticket.UserId,
                cancellationToken);

        if (identity is null ||
            !identity.IsActive ||
            !string.Equals(
                identity.SecurityStamp,
                ticket.SecurityStamp,
                StringComparison.Ordinal))
        {
            throw new AuthenticationFailedException();
        }

        bool isMember = await organizationMembershipReader.IsActiveMemberAsync(
            identity.UserId,
            organizationId,
            cancellationToken);

        if (!isMember)
        {
            throw new AuthenticationFailedException();
        }

        AuthenticationResultDto profile = await profileBuilder.BuildAsync(
            identity,
            IdentityClaimNames.ScopeOrganization,
            organizationId,
            cancellationToken);

        await sessionWriter.WriteAsync(
            new AuthenticationSession(
                identity.UserId,
                identity.Email,
                identity.DisplayName,
                IdentityClaimNames.ScopeOrganization,
                organizationId,
                identity.SecurityStamp),
            cancellationToken);

        return LoginResult.Authenticated(profile);
    }
}
