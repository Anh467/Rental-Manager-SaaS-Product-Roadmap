using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Contracts;

namespace RentalManager.Modules.Identity.Application.Authentication;

/// <summary>
/// Turns an already-authenticated user into a session. Every way of signing in
/// funnels through here, so scope selection, the organization-selection ticket
/// and the security events that go with them exist in exactly one place.
/// </summary>
public sealed class AuthenticationSessionIssuer(
    IOrganizationMembershipReader organizationMembershipReader,
    IProtectedOrganizationSelectionTicketService selectionTicketService,
    IAuthenticationSessionWriter sessionWriter,
    AuthenticationProfileBuilder profileBuilder,
    IPlatformPermissionReader platformPermissionReader,
    ISecurityEventPublisher securityEvents,
    IOrganizationContext organizationContext,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan SelectionTicketLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Issues the session the user's memberships allow: a global session for a
    /// platform role, an organization session for a single membership, or an
    /// organization-selection ticket when the user must choose.
    /// </summary>
    public async Task<LoginResult> IssueAsync(
        AuthenticatedIdentity identity,
        string? provider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);

        bool hasPlatformRole = await platformPermissionReader.HasAnyPlatformRoleAsync(
            identity.UserId,
            cancellationToken);

        if (hasPlatformRole || identity.GlobalRoleId is not null)
        {
            return await WriteSessionAsync(
                identity,
                provider,
                IdentityClaimNames.ScopeGlobal,
                organizationId: null,
                staffMembershipId: null,
                cancellationToken);
        }

        IReadOnlyList<OrganizationOptionDto> organizations =
            await organizationMembershipReader.ListActiveOrganizationsAsync(
                identity.UserId,
                cancellationToken);

        if (organizations.Count == 0)
        {
            await PublishLoginFailedAsync(
                provider,
                SecurityEventReasons.NoActiveMembership,
                identity.UserId,
                cancellationToken);

            return LoginResult.Failed();
        }

        if (organizations.Count > 1)
        {
            DateTimeOffset issuedAt = timeProvider.GetUtcNow();

            string ticket = selectionTicketService.Protect(
                new OrganizationSelectionTicket(
                    identity.UserId,
                    identity.SecurityStamp,
                    issuedAt,
                    issuedAt.Add(SelectionTicketLifetime),
                    provider));

            return LoginResult.OrganizationSelectionRequired(organizations, ticket);
        }

        Guid organizationId = organizations[0].Id;
        Guid? staffMembershipId =
            await organizationMembershipReader.GetActiveStaffMembershipIdAsync(
                identity.UserId,
                organizationId,
                cancellationToken);

        return await WriteSessionAsync(
            identity,
            provider,
            IdentityClaimNames.ScopeOrganization,
            organizationId,
            staffMembershipId,
            cancellationToken);
    }

    /// <summary>
    /// Issues an organization session for a membership the caller has already
    /// verified.
    /// </summary>
    public async Task<LoginResult> IssueForOrganizationAsync(
        AuthenticatedIdentity identity,
        string? provider,
        Guid organizationId,
        Guid staffMembershipId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return await WriteSessionAsync(
            identity,
            provider,
            IdentityClaimNames.ScopeOrganization,
            organizationId,
            staffMembershipId,
            cancellationToken);
    }

    public Task PublishLoginFailedAsync(
        string? provider,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object?>
        {
            [SecurityEventFields.Provider] = provider,
            [SecurityEventFields.Reason] = reason
        };

        if (userId is Guid id)
        {
            data[SecurityEventFields.UserId] = id;
        }

        return PublishAsync(SecurityEventTypes.LoginFailed, data, cancellationToken);
    }

    public Task PublishAsync(
        string eventType,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken) =>
        securityEvents.PublishAsync(
            SecurityEvent.Create(eventType, organizationContext.CorrelationId, data),
            cancellationToken);

    private async Task<LoginResult> WriteSessionAsync(
        AuthenticatedIdentity identity,
        string? provider,
        string scope,
        Guid? organizationId,
        Guid? staffMembershipId,
        CancellationToken cancellationToken)
    {
        if (organizationId is Guid orgId && staffMembershipId is null)
        {
            staffMembershipId =
                await organizationMembershipReader.GetActiveStaffMembershipIdAsync(
                    identity.UserId,
                    orgId,
                    cancellationToken);
        }

        AuthenticationResultDto profile = await profileBuilder.BuildAsync(
            identity,
            scope,
            organizationId,
            staffMembershipId,
            cancellationToken);

        await sessionWriter.WriteAsync(
            new AuthenticationSession(
                identity.UserId,
                identity.Email,
                identity.DisplayName,
                scope,
                organizationId,
                identity.SecurityStamp,
                provider,
                staffMembershipId),
            cancellationToken);

        await PublishAsync(
            SecurityEventTypes.LoginSucceeded,
            new Dictionary<string, object?>
            {
                [SecurityEventFields.UserId] = identity.UserId,
                [SecurityEventFields.Provider] = provider,
                [SecurityEventFields.Scope] = scope,
                [SecurityEventFields.OrganizationId] = organizationId
            },
            cancellationToken);

        return LoginResult.Authenticated(profile);
    }
}
