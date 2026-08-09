namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// Resolves PlatformRole permissions for a user. Used for global-scope
/// authorization; organization permissions stay on <c>IPermissionReader</c>.
/// </summary>
public interface IPlatformPermissionReader
{
    Task<bool> HasAnyPlatformRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PlatformRoleSummary?> GetPrimaryRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformRoleSummary(string Key, string Name);
