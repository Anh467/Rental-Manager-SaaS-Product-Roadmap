using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.Authentication.SelectOrganization;

public sealed class SelectOrganizationCommandHandler(
    IProtectedOrganizationSelectionTicketService selectionTicketService,
    IUserAccountStore users,
    IOrganizationMembershipReader organizationMembershipReader,
    AuthenticationSessionIssuer sessionIssuer,
    TimeProvider timeProvider)
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

        if (ticket is null || ticket.ExpiresAt <= timeProvider.GetUtcNow())
        {
            throw new AuthenticationFailedException();
        }

        AuthenticatedIdentity? identity =
            await users.FindActiveByIdAsync(ticket.UserId, cancellationToken);

        if (identity is null ||
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

        return await sessionIssuer.IssueForOrganizationAsync(
            identity,
            ticket.Provider,
            organizationId,
            cancellationToken);
    }
}
