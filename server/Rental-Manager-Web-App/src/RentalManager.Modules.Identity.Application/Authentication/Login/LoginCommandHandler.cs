using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.Authentication.Login;

public sealed class LoginCommandHandler(
    ICredentialAuthenticator credentialAuthenticator,
    IOrganizationMembershipReader organizationMembershipReader,
    IProtectedOrganizationSelectionTicketService selectionTicketService,
    IAuthenticationSessionWriter sessionWriter,
    AuthenticationProfileBuilder profileBuilder)
    : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email) ||
            string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ValidationFailedException(
                nameof(command.Email),
                MessageCode.Error.ValidationFailed);
        }

        CredentialAuthenticationResult authentication =
            await credentialAuthenticator.AuthenticateAsync(
                command.Email,
                command.Password,
                cancellationToken);

        if (!authentication.Succeeded || authentication.Identity is null)
        {
            return LoginResult.Failed();
        }

        AuthenticatedIdentity identity = authentication.Identity;

        if (identity.GlobalRoleId is not null)
        {
            AuthenticationResultDto profile = await profileBuilder.BuildAsync(
                identity,
                IdentityClaimNames.ScopeGlobal,
                organizationId: null,
                cancellationToken);

            await sessionWriter.WriteAsync(
                new AuthenticationSession(
                    identity.UserId,
                    identity.Email,
                    identity.DisplayName,
                    IdentityClaimNames.ScopeGlobal,
                    ActiveOrganizationId: null,
                    identity.SecurityStamp),
                cancellationToken);

            return LoginResult.Authenticated(profile);
        }

        IReadOnlyList<OrganizationOptionDto> organizations =
            await organizationMembershipReader.ListActiveOrganizationsAsync(
                identity.UserId,
                cancellationToken);

        if (organizations.Count == 0)
        {
            return LoginResult.Failed();
        }

        if (organizations.Count > 1)
        {
            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            string ticket = selectionTicketService.Protect(
                new OrganizationSelectionTicket(
                    identity.UserId,
                    identity.SecurityStamp,
                    issuedAt,
                    issuedAt.Add(TimeSpan.FromMinutes(5))));

            return LoginResult.OrganizationSelectionRequired(organizations, ticket);
        }

        Guid organizationId = organizations[0].Id;
        AuthenticationResultDto orgProfile = await profileBuilder.BuildAsync(
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

        return LoginResult.Authenticated(orgProfile);
    }
}
