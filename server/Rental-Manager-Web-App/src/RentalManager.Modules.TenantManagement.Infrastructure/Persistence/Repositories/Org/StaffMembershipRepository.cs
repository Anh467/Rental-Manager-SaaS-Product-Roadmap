using System.Data;
using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Org;

/// <summary>
/// Staff membership persistence. Scoped by the trusted organization context and
/// row level security; never trusts a client-supplied organization.
/// </summary>
public sealed class StaffMembershipRepository : IStaffMembershipRepository
{
    private readonly ISqlExecutionContext _executionContext;
    private readonly IOrganizationContext _organizationContext;

    public StaffMembershipRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(organizationContext);

        _executionContext = executionContext;
        _organizationContext = organizationContext;
    }

    public async Task<StaffMembership?> FindActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = RequireOrganizationId();
        var parameters = CreateParameters(userId, organizationId);

        const string sql = """
            SELECT
                membership.[Id],
                membership.[OrganizationId],
                membership.[UserId],
                membership.[Status],
                membership.[CreatedAt],
                membership.[UpdatedAt],
                membership.[DeletedAt],
                membership.[RowVersion]
            FROM [org].[StaffMembership] AS membership
            INNER JOIN [dbo].[User] AS [user]
                ON [user].[Id] = membership.[UserId]
            INNER JOIN [dbo].[Organization] AS organization
                ON organization.[Id] = membership.[OrganizationId]
            WHERE membership.[OrganizationId] = @OrganizationId
              AND membership.[UserId] = @UserId
              AND membership.[Status] = 2
              AND membership.[DeletedAt] IS NULL
              AND [user].[IsActive] = 1
              AND [user].[DeletedAt] IS NULL
              AND organization.[IsActive] = 1
              AND organization.[DeletedAt] IS NULL
              AND EXISTS
              (
                  SELECT 1
                  FROM [org].[StaffRole] AS staffRole
                  INNER JOIN [org].[Role] AS role
                      ON role.[Id] = staffRole.[RoleId]
                     AND role.[OrganizationId] = staffRole.[OrganizationId]
                  WHERE staffRole.[StaffMembershipId] = membership.[Id]
                    AND staffRole.[OrganizationId] = membership.[OrganizationId]
                    AND role.[IsActive] = 1
                    AND role.[DeletedAt] IS NULL
              );
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        return await execution.Connection.QuerySingleOrDefaultAsync<StaffMembership>(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<Guid?> GetActiveStaffMembershipIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        StaffMembership? membership = await FindActiveByUserIdAsync(
            userId,
            cancellationToken);

        return membership?.Id;
    }

    public async Task<IReadOnlyList<ActiveOrganizationMembership>> ListActiveMembershipsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Guid);

        const string sql = """
            EXEC [dbo].[usp_ListActiveOrganizationMembershipsForUser] @UserId = @UserId;
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        IEnumerable<ActiveOrganizationMembership> memberships =
            await execution.Connection.QueryAsync<ActiveOrganizationMembership>(
                new CommandDefinition(
                    sql,
                    parameters,
                    execution.Transaction,
                    cancellationToken: cancellationToken));

        return memberships.ToArray();
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = RequireOrganizationId();
        var parameters = CreateParameters(userId, organizationId);

        const string sql = """
            SELECT DISTINCT permission.[Key]
            FROM [org].[StaffMembership] AS membership
            INNER JOIN [org].[StaffRole] AS staffRole
                ON staffRole.[StaffMembershipId] = membership.[Id]
               AND staffRole.[OrganizationId] = membership.[OrganizationId]
            INNER JOIN [org].[Role] AS role
                ON role.[Id] = staffRole.[RoleId]
               AND role.[OrganizationId] = membership.[OrganizationId]
            INNER JOIN [org].[RolePermission] AS rolePermission
                ON rolePermission.[RoleId] = role.[Id]
               AND rolePermission.[OrganizationId] = membership.[OrganizationId]
            INNER JOIN [dbo].[Permission] AS permission
                ON permission.[Id] = rolePermission.[PermissionId]
            INNER JOIN [dbo].[User] AS [user]
                ON [user].[Id] = membership.[UserId]
            INNER JOIN [dbo].[Organization] AS organization
                ON organization.[Id] = membership.[OrganizationId]
            WHERE membership.[OrganizationId] = @OrganizationId
              AND membership.[UserId] = @UserId
              AND membership.[Status] = 2
              AND membership.[DeletedAt] IS NULL
              AND [user].[IsActive] = 1
              AND [user].[DeletedAt] IS NULL
              AND organization.[IsActive] = 1
              AND organization.[DeletedAt] IS NULL
              AND role.[IsActive] = 1
              AND role.[DeletedAt] IS NULL
              AND permission.[IsActive] = 1
            ORDER BY permission.[Key];
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        IEnumerable<string> keys = await execution.Connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));

        return keys.ToArray();
    }

    public async Task SaveAsync(
        StaffMembership membership,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);

        Guid organizationId = RequireOrganizationId();
        membership.OrganizationId = organizationId;

        if (membership.Id == Guid.Empty)
        {
            membership.Id = Guid.CreateVersion7();
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (membership.CreatedAt == default)
        {
            membership.CreatedAt = now;
        }

        membership.UpdatedAt = now;

        var parameters = new DynamicParameters();
        parameters.Add("Id", membership.Id, DbType.Guid);
        parameters.Add("OrganizationId", membership.OrganizationId, DbType.Guid);
        parameters.Add("UserId", membership.UserId, DbType.Guid);
        parameters.Add("Status", membership.Status, DbType.Byte);
        parameters.Add("CreatedAt", membership.CreatedAt);
        parameters.Add("UpdatedAt", membership.UpdatedAt);
        parameters.Add("DeletedAt", membership.DeletedAt);

        const string sql = """
            MERGE [org].[StaffMembership] AS target
            USING
            (
                SELECT
                    @Id AS [Id],
                    @OrganizationId AS [OrganizationId],
                    @UserId AS [UserId],
                    @Status AS [Status],
                    @CreatedAt AS [CreatedAt],
                    @UpdatedAt AS [UpdatedAt],
                    @DeletedAt AS [DeletedAt]
            ) AS source
            ON target.[OrganizationId] = source.[OrganizationId]
               AND target.[UserId] = source.[UserId]
               AND target.[DeletedAt] IS NULL
            WHEN MATCHED THEN
                UPDATE SET
                    [Status] = source.[Status],
                    [UpdatedAt] = source.[UpdatedAt],
                    [DeletedAt] = source.[DeletedAt]
            WHEN NOT MATCHED THEN
                INSERT
                (
                    [Id],
                    [OrganizationId],
                    [UserId],
                    [Status],
                    [CreatedAt],
                    [UpdatedAt],
                    [DeletedAt]
                )
                VALUES
                (
                    source.[Id],
                    source.[OrganizationId],
                    source.[UserId],
                    source.[Status],
                    source.[CreatedAt],
                    source.[UpdatedAt],
                    source.[DeletedAt]
                );
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task AssignRoleAsync(
        Guid staffMembershipId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = RequireOrganizationId();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var parameters = new DynamicParameters();
        parameters.Add("Id", Guid.CreateVersion7(), DbType.Guid);
        parameters.Add("OrganizationId", organizationId, DbType.Guid);
        parameters.Add("StaffMembershipId", staffMembershipId, DbType.Guid);
        parameters.Add("RoleId", roleId, DbType.Guid);
        parameters.Add("CreatedAt", now);
        parameters.Add("UpdatedAt", now);

        const string sql = """
            IF NOT EXISTS
            (
                SELECT 1
                FROM [org].[StaffRole]
                WHERE [StaffMembershipId] = @StaffMembershipId
                  AND [RoleId] = @RoleId
            )
            BEGIN
                INSERT INTO [org].[StaffRole]
                (
                    [Id],
                    [OrganizationId],
                    [StaffMembershipId],
                    [RoleId],
                    [CreatedAt],
                    [UpdatedAt]
                )
                VALUES
                (
                    @Id,
                    @OrganizationId,
                    @StaffMembershipId,
                    @RoleId,
                    @CreatedAt,
                    @UpdatedAt
                );
            END;
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task SoftDeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = RequireOrganizationId();
        DynamicParameters parameters = CreateParameters(userId, organizationId);
        parameters.Add("UpdatedAt", DateTimeOffset.UtcNow);
        parameters.Add("DeletedAt", DateTimeOffset.UtcNow);

        const string sql = """
            UPDATE [org].[StaffMembership]
            SET
                [Status] = 3,
                [UpdatedAt] = @UpdatedAt,
                [DeletedAt] = @DeletedAt
            WHERE [OrganizationId] = @OrganizationId
              AND [UserId] = @UserId
              AND [DeletedAt] IS NULL;
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    private Guid RequireOrganizationId()
    {
        return _organizationContext.OrganizationId
            ?? throw new MissingOrganizationContextException();
    }

    private static DynamicParameters CreateParameters(
        Guid userId,
        Guid organizationId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Guid);
        parameters.Add("OrganizationId", organizationId, DbType.Guid);
        return parameters;
    }
}
