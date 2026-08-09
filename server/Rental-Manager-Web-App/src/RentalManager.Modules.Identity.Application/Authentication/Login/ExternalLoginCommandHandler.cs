using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// The single onboarding and sign-in path for external identities.
/// <c>(Provider, Subject)</c> is the identity key; email is only a profile
/// attribute, so an email that already belongs to a different account is a
/// conflict to report rather than an account to silently link.
/// </summary>
public sealed class ExternalLoginCommandHandler(
    IExternalIdentityResolver identityResolver,
    IUserAccountStore users,
    IExternalAuthenticationPolicy policy,
    AuthenticationSessionIssuer sessionIssuer,
    TimeProvider timeProvider)
    : ICommandHandler<ExternalLoginCommand, LoginResult>
{
    private const string ProviderField = "provider";
    private const string SubjectField = "subject";
    private const string EmailField = "email";

    public async Task<LoginResult> HandleAsync(
        ExternalLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // The request only ever carries a hint; what the deployment accepts as a
        // verified identity is the resolver's decision.
        ExternalIdentityDescriptor? resolved = await identityResolver.ResolveAsync(
            new ExternalIdentityDescriptor(
                command.Provider ?? string.Empty,
                command.Subject ?? string.Empty,
                command.Email,
                command.DisplayName),
            cancellationToken);

        if (resolved is null)
        {
            await sessionIssuer.PublishLoginFailedAsync(
                provider: null,
                SecurityEventReasons.ExternalPrincipalMissing,
                userId: null,
                cancellationToken);

            throw new AuthenticationFailedException();
        }

        string provider = resolved.Provider.Trim();
        string subject = resolved.Subject.Trim();

        if (provider.Length == 0)
        {
            throw new ValidationFailedException(
                ProviderField,
                MessageCode.Error.ValidationFailed);
        }

        if (subject.Length == 0)
        {
            await sessionIssuer.PublishLoginFailedAsync(
                provider,
                SecurityEventReasons.SubjectMissing,
                userId: null,
                cancellationToken);

            throw new ValidationFailedException(
                SubjectField,
                MessageCode.Error.ValidationFailed);
        }

        if (!policy.IsProviderAllowed(provider))
        {
            await sessionIssuer.PublishLoginFailedAsync(
                provider,
                SecurityEventReasons.ProviderNotAllowed,
                userId: null,
                cancellationToken);

            throw new ValidationFailedException(
                ProviderField,
                MessageCode.Error.ValidationFailed);
        }

        ExternalIdentityMapping mapping =
            await users.FindByExternalIdentityAsync(provider, subject, cancellationToken)
            ?? await ProvisionAsync(provider, subject, resolved, cancellationToken);

        await EnsureActiveAsync(mapping, cancellationToken);

        await users.RecordLoginAsync(
            mapping.Id,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return await sessionIssuer.IssueAsync(
            mapping.User,
            mapping.Provider,
            cancellationToken);
    }

    private async Task<ExternalIdentityMapping> ProvisionAsync(
        string provider,
        string subject,
        ExternalIdentityDescriptor identity,
        CancellationToken cancellationToken)
    {
        if (!policy.AllowsFirstLoginProvisioning(provider))
        {
            await sessionIssuer.PublishLoginFailedAsync(
                provider,
                SecurityEventReasons.ProviderNotAllowed,
                userId: null,
                cancellationToken);

            throw new AuthenticationFailedException();
        }

        string email = (identity.Email ?? string.Empty).Trim();

        if (email.Length == 0)
        {
            await sessionIssuer.PublishLoginFailedAsync(
                provider,
                SecurityEventReasons.EmailMissing,
                userId: null,
                cancellationToken);

            throw new ValidationFailedException(
                EmailField,
                MessageCode.Error.ValidationFailed);
        }

        // Checked up front so the common conflict is reported without relying on
        // a unique-constraint failure; the store still enforces it under a race.
        if (await users.IsEmailInUseAsync(email, cancellationToken))
        {
            await PublishIdentityConflictAsync(provider, cancellationToken);
            throw new ExternalIdentityConflictException(MessageCode.ObjectName.User);
        }

        string displayName = (identity.DisplayName ?? string.Empty).Trim();

        ExternalUserProvisionResult result = await users.ProvisionAsync(
            new ExternalUserProvisionRequest(
                provider,
                subject,
                email,
                displayName.Length == 0 ? email : displayName),
            cancellationToken);

        if (result.Status == ExternalUserProvisionStatus.EmailConflict)
        {
            await PublishIdentityConflictAsync(provider, cancellationToken);
            throw new ExternalIdentityConflictException(MessageCode.ObjectName.User);
        }

        ExternalIdentityMapping mapping = result.Mapping
            ?? throw new InvalidOperationException(
                "Provisioning reported success without returning an identity mapping.");

        if (result.Status == ExternalUserProvisionStatus.Created)
        {
            await sessionIssuer.PublishAsync(
                SecurityEventTypes.UserProvisioned,
                new Dictionary<string, object?>
                {
                    [SecurityEventFields.UserId] = mapping.User.UserId,
                    [SecurityEventFields.Provider] = mapping.Provider
                },
                cancellationToken);
        }

        return mapping;
    }

    private async Task EnsureActiveAsync(
        ExternalIdentityMapping mapping,
        CancellationToken cancellationToken)
    {
        if (mapping.User.IsActive)
        {
            return;
        }

        await sessionIssuer.PublishAsync(
            SecurityEventTypes.InactiveUserRejected,
            new Dictionary<string, object?>
            {
                [SecurityEventFields.UserId] = mapping.User.UserId,
                [SecurityEventFields.Provider] = mapping.Provider
            },
            cancellationToken);

        await sessionIssuer.PublishLoginFailedAsync(
            mapping.Provider,
            SecurityEventReasons.UserInactive,
            mapping.User.UserId,
            cancellationToken);

        // Reactivation is never a side effect of signing in.
        throw new InactiveResourceException(MessageCode.ObjectName.User);
    }

    private Task PublishIdentityConflictAsync(
        string provider,
        CancellationToken cancellationToken)
    {
        return sessionIssuer.PublishAsync(
            SecurityEventTypes.IdentityConflict,
            new Dictionary<string, object?>
            {
                [SecurityEventFields.Provider] = provider,
                [SecurityEventFields.Object] = MessageCode.ObjectName.User,
                [SecurityEventFields.Reason] = SecurityEventReasons.EmailAlreadyLinked
            },
            cancellationToken);
    }
}
