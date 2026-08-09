using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.PlatformUsers;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

public sealed class SqlPlatformUserStore(IIdentityConnectionFactory connectionFactory)
    : IPlatformUserStore, IUserSessionStateReader
{
    public async Task<PlatformUserRecord?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PlatformUserRecord>(
            new CommandDefinition(
                PlatformUserSql.GetById,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    public async Task<PlatformUserListResult> ListAsync(
        PlatformUserListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("Search", string.IsNullOrWhiteSpace(query.Search)
            ? null
            : $"%{query.Search.Trim()}%");
        parameters.Add("IsActive", query.IsActive, DbType.Boolean);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize, DbType.Int32);
        parameters.Add("PageSize", query.PageSize, DbType.Int32);

        await using SqlMapper.GridReader multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                PlatformUserSql.List,
                parameters,
                cancellationToken: cancellationToken));

        int total = await multi.ReadSingleAsync<int>();
        IReadOnlyList<PlatformUserRecord> items =
            (await multi.ReadAsync<PlatformUserRecord>()).ToArray();

        return new PlatformUserListResult(items, total, query.Page, query.PageSize);
    }

    public async Task<PlatformUserRecord> UpdateAsync(
        Guid userId,
        string displayName,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(expectedRowVersion);

        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        PlatformUserRecord? updated = await connection.QuerySingleOrDefaultAsync<PlatformUserRecord>(
            new CommandDefinition(
                PlatformUserSql.UpdateDisplayName,
                new
                {
                    UserId = userId,
                    DisplayName = displayName.Trim(),
                    ExpectedRowVersion = expectedRowVersion,
                    Now = DateTimeOffset.UtcNow
                },
                cancellationToken: cancellationToken));

        if (updated is not null)
        {
            return updated;
        }

        await ThrowWriteFailureAsync(connection, userId, expectedRowVersion, cancellationToken);
        throw new InvalidOperationException("Unreachable");
    }

    public async Task<PlatformUserRecord> SetActiveAsync(
        Guid userId,
        bool isActive,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedRowVersion);

        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        // Rotate SecurityStamp on inactivate so existing cookies fail
        // OnValidatePrincipal immediately.
        string? securityStamp = isActive ? null : Guid.NewGuid().ToString();

        PlatformUserRecord? updated = await connection.QuerySingleOrDefaultAsync<PlatformUserRecord>(
            new CommandDefinition(
                PlatformUserSql.SetActive,
                new
                {
                    UserId = userId,
                    IsActive = isActive,
                    SecurityStamp = securityStamp,
                    ExpectedRowVersion = expectedRowVersion,
                    Now = DateTimeOffset.UtcNow
                },
                cancellationToken: cancellationToken));

        if (updated is not null)
        {
            return updated;
        }

        await ThrowWriteFailureAsync(connection, userId, expectedRowVersion, cancellationToken);
        throw new InvalidOperationException("Unreachable");
    }

    public async Task<int> CountOrganizationMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM [org].[OrganizationUser]
                WHERE [UserId] = @UserId
                  AND [DeletedAt] IS NULL;
                """,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    public async Task AssignPlatformRoleAsync(
        Guid userId,
        Guid platformRoleId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM [dbo].[PlatformUserRole]
                    WHERE [UserId] = @UserId
                      AND [PlatformRoleId] = @PlatformRoleId
                )
                BEGIN
                    INSERT INTO [dbo].[PlatformUserRole] ([UserId], [PlatformRoleId], [CreatedAt])
                    VALUES (@UserId, @PlatformRoleId, SYSUTCDATETIME());
                END
                """,
                new { UserId = userId, PlatformRoleId = platformRoleId },
                cancellationToken: cancellationToken));
    }

    public async Task<UserSessionState?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UserSessionState>(
            new CommandDefinition(
                """
                SELECT
                    [Id] AS [UserId],
                    [IsActive],
                    [SecurityStamp],
                    [DeletedAt]
                FROM [dbo].[User]
                WHERE [Id] = @UserId;
                """,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    private static async Task ThrowWriteFailureAsync(
        SqlConnection connection,
        Guid userId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        PlatformUserRecord? current = await connection.QuerySingleOrDefaultAsync<PlatformUserRecord>(
            new CommandDefinition(
                PlatformUserSql.GetById,
                new { UserId = userId },
                cancellationToken: cancellationToken));

        if (current is null)
        {
            throw new ResourceNotFoundException(PlatformUserInvariants.ObjectName);
        }

        if (!current.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new ConcurrencyConflictException(PlatformUserInvariants.ObjectName);
        }

        throw new ResourceNotFoundException(PlatformUserInvariants.ObjectName);
    }

    private static class PlatformUserSql
    {
        private const string SelectColumns = """
            [Id],
            [Email],
            [DisplayName],
            [IsActive],
            [GlobalRoleId],
            [CreatedAt],
            [UpdatedAt],
            [RowVersion]
            """;

        public const string GetById = $"""
            SELECT {SelectColumns}
            FROM [dbo].[User]
            WHERE [Id] = @UserId
              AND [DeletedAt] IS NULL;
            """;

        public const string List = $"""
            SELECT COUNT(1)
            FROM [dbo].[User]
            WHERE [DeletedAt] IS NULL
              AND (@IsActive IS NULL OR [IsActive] = @IsActive)
              AND (
                    @Search IS NULL
                    OR [Email] LIKE @Search
                    OR [DisplayName] LIKE @Search
                  );

            SELECT {SelectColumns}
            FROM [dbo].[User]
            WHERE [DeletedAt] IS NULL
              AND (@IsActive IS NULL OR [IsActive] = @IsActive)
              AND (
                    @Search IS NULL
                    OR [Email] LIKE @Search
                    OR [DisplayName] LIKE @Search
                  )
            ORDER BY [DisplayName], [Email]
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        public const string UpdateDisplayName = $"""
            UPDATE [dbo].[User]
            SET
                [DisplayName] = @DisplayName,
                [UpdatedAt] = @Now,
                [ConcurrencyStamp] = CONVERT(NVARCHAR(36), NEWID())
            OUTPUT
                inserted.[Id],
                inserted.[Email],
                inserted.[DisplayName],
                inserted.[IsActive],
                inserted.[GlobalRoleId],
                inserted.[CreatedAt],
                inserted.[UpdatedAt],
                inserted.[RowVersion]
            WHERE [Id] = @UserId
              AND [DeletedAt] IS NULL
              AND [RowVersion] = @ExpectedRowVersion;
            """;

        public const string SetActive = $"""
            UPDATE [dbo].[User]
            SET
                [IsActive] = @IsActive,
                [SecurityStamp] = COALESCE(@SecurityStamp, [SecurityStamp]),
                [UpdatedAt] = @Now,
                [ConcurrencyStamp] = CONVERT(NVARCHAR(36), NEWID())
            OUTPUT
                inserted.[Id],
                inserted.[Email],
                inserted.[DisplayName],
                inserted.[IsActive],
                inserted.[GlobalRoleId],
                inserted.[CreatedAt],
                inserted.[UpdatedAt],
                inserted.[RowVersion]
            WHERE [Id] = @UserId
              AND [DeletedAt] IS NULL
              AND [RowVersion] = @ExpectedRowVersion;
            """;
    }
}
