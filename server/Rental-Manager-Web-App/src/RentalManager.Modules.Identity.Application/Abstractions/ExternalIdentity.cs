namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// A verified external identity as presented by the authentication middleware.
/// <paramref name="Provider"/> is the stable provider key configured for the
/// deployment and <paramref name="Subject"/> is the provider's <c>sub</c> claim.
/// Email and display name are profile attributes only: they are never used as
/// the identity key.
/// </summary>
public sealed record ExternalIdentityDescriptor(
    string Provider,
    string Subject,
    string? Email,
    string? DisplayName);

/// <summary>
/// A stored <c>(Provider, Subject)</c> to user mapping together with the user it
/// resolves to. The user is carried even when inactive so the caller can refuse
/// the login with the correct message instead of pretending the account is
/// missing.
/// </summary>
public sealed record ExternalIdentityMapping(
    Guid Id,
    string Provider,
    AuthenticatedIdentity User);

/// <summary>
/// Everything needed to create a user and its first identity mapping in one
/// transaction. <paramref name="GlobalRoleId"/> is only supplied by controlled
/// bootstrap; ordinary first-login provisioning never grants a role.
/// </summary>
public sealed record ExternalUserProvisionRequest(
    string Provider,
    string Subject,
    string Email,
    string DisplayName,
    Guid? GlobalRoleId = null);

public enum ExternalUserProvisionStatus
{
    /// <summary>The user and its identity mapping were created.</summary>
    Created = 0,

    /// <summary>
    /// A concurrent request created the same <c>(Provider, Subject)</c> mapping
    /// first. The existing mapping is returned so provisioning is idempotent.
    /// </summary>
    AlreadyMapped = 1,

    /// <summary>
    /// The email belongs to another user. Automatic linking is not allowed, so
    /// nothing was written.
    /// </summary>
    EmailConflict = 2
}

public sealed record ExternalUserProvisionResult(
    ExternalUserProvisionStatus Status,
    ExternalIdentityMapping? Mapping)
{
    public static ExternalUserProvisionResult Created(ExternalIdentityMapping mapping) =>
        new(ExternalUserProvisionStatus.Created, mapping);

    public static ExternalUserProvisionResult AlreadyMapped(ExternalIdentityMapping mapping) =>
        new(ExternalUserProvisionStatus.AlreadyMapped, mapping);

    public static ExternalUserProvisionResult EmailConflict() =>
        new(ExternalUserProvisionStatus.EmailConflict, null);
}
