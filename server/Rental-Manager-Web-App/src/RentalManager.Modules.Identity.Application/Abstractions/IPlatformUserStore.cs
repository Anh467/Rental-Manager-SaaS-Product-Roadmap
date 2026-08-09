namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// Platform User lifecycle persistence for Identity use cases.
/// </summary>
public interface IPlatformUserStore
{
    Task<PlatformUserRecord?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PlatformUserListResult> ListAsync(
        PlatformUserListQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformUserRecord> UpdateAsync(
        Guid userId,
        string displayName,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets <c>IsActive</c>, rotates <c>SecurityStamp</c> when inactivating so
    /// existing sessions fail validation, and enforces RowVersion concurrency.
    /// </summary>
    Task<PlatformUserRecord> SetActiveAsync(
        Guid userId,
        bool isActive,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<int> CountOrganizationMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AssignPlatformRoleAsync(
        Guid userId,
        Guid platformRoleId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight read model used by session validation middleware.
/// </summary>
public interface IUserSessionStateReader
{
    Task<UserSessionState?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record UserSessionState(
    Guid UserId,
    bool IsActive,
    string SecurityStamp,
    DateTimeOffset? DeletedAt);

public sealed record PlatformUserRecord(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    Guid? GlobalRoleId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    byte[] RowVersion);

public sealed record PlatformUserListQuery(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);

public sealed record PlatformUserListResult(
    IReadOnlyList<PlatformUserRecord> Items,
    int TotalCount,
    int Page,
    int PageSize);
