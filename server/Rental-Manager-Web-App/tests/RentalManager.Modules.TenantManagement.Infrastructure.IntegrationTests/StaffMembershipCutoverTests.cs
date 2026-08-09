using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Upgrade cutover from residual OrganizationUser rows into StaffMembership +
/// StaffRole. Uses the named SCRUM-81 cutover script against a fixture database
/// that already has the DACPAC model (including residual OrganizationUser).
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class StaffMembershipCutoverTests
{
    private readonly SqlServerFixture _fixture;

    public StaffMembershipCutoverTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Cutover_migrates_multi_org_rows_with_policies_on_before_and_after()
    {
        Assert.True(await IsIsolationPolicyEnabledAsync());

        Guid userId = Guid.CreateVersion7();
        string email = $"cutover.multi.{userId:N}@rentalmanager.test";
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await SeedUserAsync(userId, email, now);

        // Seed residual OrganizationUser rows in two orgs without leaving a
        // session context for the cutover connection.
        await SeedOrganizationUserAsync(
            TestData.OrganizationA.Id,
            userId,
            TestData.OrganizationA.AdministratorRoleId,
            now);
        await SeedOrganizationUserAsync(
            TestData.OrganizationB.Id,
            userId,
            TestData.OrganizationB.AdministratorRoleId,
            now);

        string cutoverSql = await File.ReadAllTextAsync(ResolveCutoverScriptPath());

        await using (SqlConnection connection = await _fixture.OpenConnectionAsync())
        {
            // No organization SESSION_CONTEXT — cutover must disable RLS itself.
            await connection.ExecuteAsync(cutoverSql);
            await connection.ExecuteAsync(cutoverSql);
        }

        Assert.True(await IsIsolationPolicyEnabledAsync());

        await using SqlConnection verify = await _fixture.OpenConnectionAsync();
        await verify.ExecuteAsync(
            """
            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = OFF);
            """);

        try
        {
            int membershipCount = await verify.ExecuteScalarAsync<int>(
                """
                SELECT COUNT_BIG(1)
                FROM [org].[StaffMembership]
                WHERE [UserId] = @UserId
                  AND [DeletedAt] IS NULL;
                """,
                new { UserId = userId });

            Assert.Equal(2, membershipCount);

            int staffRoleCount = await verify.ExecuteScalarAsync<int>(
                """
                SELECT COUNT_BIG(1)
                FROM [org].[StaffRole] AS staffRole
                INNER JOIN [org].[StaffMembership] AS membership
                    ON membership.[Id] = staffRole.[StaffMembershipId]
                   AND membership.[OrganizationId] = staffRole.[OrganizationId]
                WHERE membership.[UserId] = @UserId;
                """,
                new { UserId = userId });

            Assert.Equal(2, staffRoleCount);

            int orphans = await verify.ExecuteScalarAsync<int>(
                """
                SELECT COUNT_BIG(1)
                FROM [org].[StaffRole] AS staffRole
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [org].[StaffMembership] AS membership
                    WHERE membership.[Id] = staffRole.[StaffMembershipId]
                      AND membership.[OrganizationId] = staffRole.[OrganizationId]
                );
                """);

            Assert.Equal(0, orphans);

            int crossOrg = await verify.ExecuteScalarAsync<int>(
                """
                SELECT COUNT_BIG(1)
                FROM [org].[StaffRole] AS staffRole
                INNER JOIN [org].[StaffMembership] AS membership
                    ON membership.[Id] = staffRole.[StaffMembershipId]
                WHERE membership.[OrganizationId] <> staffRole.[OrganizationId]
                  AND membership.[UserId] = @UserId;
                """,
                new { UserId = userId });

            Assert.Equal(0, crossOrg);

            foreach (Guid organizationId in new[]
                     {
                         TestData.OrganizationA.Id,
                         TestData.OrganizationB.Id
                     })
            {
                var membership = await verify.QuerySingleAsync<(Guid Id, byte Status)>(
                    """
                    SELECT [Id], [Status]
                    FROM [org].[StaffMembership]
                    WHERE [OrganizationId] = @OrganizationId
                      AND [UserId] = @UserId
                      AND [DeletedAt] IS NULL;
                    """,
                    new { OrganizationId = organizationId, UserId = userId });

                Assert.Equal((byte)2, membership.Status);
            }
        }
        finally
        {
            await verify.ExecuteAsync(
                """
                ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                    WITH (STATE = ON);
                """);
        }

        Assert.True(await IsIsolationPolicyEnabledAsync());
    }

    [Fact]
    public async Task Cutover_re_enables_policy_after_intentional_failure()
    {
        Assert.True(await IsIsolationPolicyEnabledAsync());

        Guid userId = Guid.CreateVersion7();
        string email = $"cutover.fail.{userId:N}@rentalmanager.test";
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await SeedUserAsync(userId, email, now);
        await SeedOrganizationUserAsync(
            TestData.OrganizationA.Id,
            userId,
            TestData.OrganizationA.AdministratorRoleId,
            now);

        // Force the cutover MERGE to fail so CATCH must re-enable RLS.
        await using (SqlConnection seed = await _fixture.OpenConnectionAsync())
        {
            await seed.ExecuteAsync(
                """
                ALTER TABLE [org].[StaffMembership] WITH NOCHECK
                    ADD CONSTRAINT [CK_SCRUM81_TestForcedFailure]
                    CHECK ([Id] <> [Id]);
                """);
        }

        try
        {
            Assert.True(await IsIsolationPolicyEnabledAsync());

            string cutoverSql = await File.ReadAllTextAsync(ResolveCutoverScriptPath());

            await using (SqlConnection connection = await _fixture.OpenConnectionAsync())
            {
                await Assert.ThrowsAsync<SqlException>(
                    () => connection.ExecuteAsync(cutoverSql));
            }

            Assert.True(await IsIsolationPolicyEnabledAsync());
        }
        finally
        {
            await using SqlConnection cleanup = await _fixture.OpenConnectionAsync();
            await cleanup.ExecuteAsync(
                """
                IF OBJECT_ID(N'[org].[CK_SCRUM81_TestForcedFailure]', N'C') IS NOT NULL
                    ALTER TABLE [org].[StaffMembership]
                        DROP CONSTRAINT [CK_SCRUM81_TestForcedFailure];
                """);
        }
    }

    private async Task SeedUserAsync(Guid userId, string email, DateTimeOffset now)
    {
        await using SqlConnection connection = await _fixture.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO [dbo].[User]
                ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
                 [EmailConfirmed], [DisplayName], [SecurityStamp], [ConcurrencyStamp],
                 [LockoutEnabled], [AccessFailedCount], [CreatedAt], [UpdatedAt])
            VALUES
                (@UserId, @Email, UPPER(@Email), @Email, UPPER(@Email), 1, N'Cutover User',
                 CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()),
                 0, 0, @Now, @Now);
            """,
            new { UserId = userId, Email = email, Now = now });
    }

    private async Task SeedOrganizationUserAsync(
        Guid organizationId,
        Guid userId,
        Guid roleId,
        DateTimeOffset now)
    {
        await using SqlConnection connection =
            await _fixture.OpenConnectionAsync(organizationId);

        await connection.ExecuteAsync(
            """
            INSERT INTO [org].[OrganizationUser]
                ([OrganizationId], [UserId], [RoleId], [IsActive], [CreatedAt], [UpdatedAt])
            VALUES
                (@OrganizationId, @UserId, @RoleId, 1, @Now, @Now);
            """,
            new
            {
                OrganizationId = organizationId,
                UserId = userId,
                RoleId = roleId,
                Now = now
            });
    }

    private async Task<bool> IsIsolationPolicyEnabledAsync()
    {
        await using SqlConnection connection = await _fixture.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT CAST(is_enabled AS BIT)
            FROM sys.security_policies
            WHERE name = N'OrganizationIsolationPolicy'
              AND schema_id = SCHEMA_ID(N'org');
            """);
    }

    private static string ResolveCutoverScriptPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "src",
                "RentalManager.Database.SQLServer",
                "Scripts",
                "Cutover",
                "SCRUM-81-StaffMembership-Cutover.sql");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "SCRUM-81-StaffMembership-Cutover.sql was not found.");
    }
}
