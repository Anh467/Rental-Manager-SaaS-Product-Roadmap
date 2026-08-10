namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// Reads and provisions platform users and their external identity mappings.
/// This is the only user persistence abstraction the identity module has; there
/// is no parallel credential store, because authentication is delegated to the
/// configured external provider.
/// </summary>
public interface IUserAccountStore
{
    /// <summary>
    /// Loads a user that is allowed to hold a session. Returns <c>null</c> for a
    /// missing, deleted or deactivated user, which callers translate into a
    /// failed authentication rather than disclosing which case applied.
    /// </summary>
    Task<AuthenticatedIdentity?> FindActiveByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a mapping by its provider-scoped subject. The subject comparison
    /// is case-sensitive, so two subjects differing only in case never resolve
    /// to the same user.
    /// </summary>
    Task<ExternalIdentityMapping?> FindByExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether any live user already owns this email address.
    /// </summary>
    Task<bool> IsEmailInUseAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the user and its identity mapping atomically. Safe to call
    /// concurrently for the same subject: exactly one caller creates and the
    /// others observe <see cref="ExternalUserProvisionStatus.AlreadyMapped"/>.
    /// </summary>
    Task<ExternalUserProvisionResult> ProvisionAsync(
        ExternalUserProvisionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the mapping was just used to authenticate.
    /// </summary>
    Task RecordLoginAsync(
        Guid userIdentityId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default);
}
