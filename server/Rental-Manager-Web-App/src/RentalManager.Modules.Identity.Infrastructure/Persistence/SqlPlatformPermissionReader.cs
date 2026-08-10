using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

public sealed class SqlPlatformPermissionReader(IIdentityConnectionFactory connectionFactory)
    : IPlatformPermissionReader
{
    public async Task<bool> HasAnyPlatformRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CAST(CASE WHEN EXISTS (
                    SELECT 1
                    FROM [dbo].[PlatformUserRole] AS assignment
                    INNER JOIN [dbo].[PlatformRole] AS role
                        ON role.[Id] = assignment.[PlatformRoleId]
                    WHERE assignment.[UserId] = @UserId
                      AND role.[IsActive] = 1
                      AND role.[DeletedAt] IS NULL
                ) THEN 1 ELSE 0 END AS BIT);
                """,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlySet<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        IEnumerable<string> keys = await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT DISTINCT permission.[Key]
                FROM [dbo].[PlatformUserRole] AS assignment
                INNER JOIN [dbo].[PlatformRole] AS role
                    ON role.[Id] = assignment.[PlatformRoleId]
                INNER JOIN [dbo].[PlatformRolePermission] AS grantRow
                    ON grantRow.[PlatformRoleId] = role.[Id]
                INNER JOIN [dbo].[Permission] AS permission
                    ON permission.[Id] = grantRow.[PermissionId]
                WHERE assignment.[UserId] = @UserId
                  AND role.[IsActive] = 1
                  AND role.[DeletedAt] IS NULL
                  AND permission.[IsActive] = 1
                  AND permission.[Scope] = 1;
                """,
                new { UserId = userId },
                cancellationToken: cancellationToken));

        return keys.ToHashSet(StringComparer.Ordinal);
    }

    public async Task<PlatformRoleSummary?> GetPrimaryRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PlatformRoleSummary>(
            new CommandDefinition(
                """
                SELECT TOP (1)
                    role.[Key],
                    role.[Name]
                FROM [dbo].[PlatformUserRole] AS assignment
                INNER JOIN [dbo].[PlatformRole] AS role
                    ON role.[Id] = assignment.[PlatformRoleId]
                WHERE assignment.[UserId] = @UserId
                  AND role.[IsActive] = 1
                  AND role.[DeletedAt] IS NULL
                ORDER BY
                    CASE WHEN role.[Key] = N'PLATFORM_SUPER_ADMIN' THEN 0 ELSE 1 END,
                    role.[Key];
                """,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }
}
