namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;

/// <summary>
/// Answers what a user may do inside the organization currently bound to the
/// session. Both questions are scoped by row level security, so a user can
/// never resolve membership or permissions for another organization.
/// </summary>
public interface IPermissionReader
{
    Task<bool> IsActiveMemberAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
