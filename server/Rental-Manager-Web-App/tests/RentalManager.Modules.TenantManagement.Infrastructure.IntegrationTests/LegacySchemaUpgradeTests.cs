using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Genuine upgrade path: build a representative legacy schema (password user,
/// GlobalRoleId admin, multi-org OrganizationUser, no Staff*/UserIdentity/
/// Platform*), publish the current DACPAC over it, run SCRUM-81 cutover, then
/// verify data and RLS. This must not start from the target DACPAC schema.
/// </summary>
public sealed class LegacySchemaUpgradeTests
{
    private const string UpgradeDatabaseName = "RentalManagerLegacyUpgradeTests";

    private const string DefaultMasterConnectionString =
        "Server=localhost,1433;Database=master;User Id=sa;" +
        "Password=RentalManager_Strong_Password_123!;" +
        "Encrypt=False;TrustServerCertificate=True";

    private static readonly Guid GlobalAdminRoleId =
        Guid.Parse("0F4FA0E8-2397-4CC3-B3A1-02189F0DCC34");

    private static readonly Guid OrgAId =
        Guid.Parse("aaaaaaaa-0000-0000-0000-0000000000a1");

    private static readonly Guid OrgBId =
        Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000b1");

    private static readonly Guid OrgARoleId =
        Guid.Parse("aaaaaaaa-0000-0000-0000-0000000000a2");

    private static readonly Guid OrgBRoleId =
        Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000b2");

    private static readonly Guid LegacyAdminUserId =
        Guid.Parse("cccccccc-0000-0000-0000-0000000000c1");

    private static readonly Guid LegacyMemberUserId =
        Guid.Parse("dddddddd-0000-0000-0000-0000000000d1");

    [Fact]
    public async Task Upgrade_from_legacy_schema_preserves_data_and_completes_cutover()
    {
        string masterConnectionString =
            Environment.GetEnvironmentVariable("RENTAL_MANAGER_TEST_CONNECTION_STRING")
            ?? DefaultMasterConnectionString;

        var masterBuilder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = "master"
        };
        masterConnectionString = masterBuilder.ConnectionString;

        await WaitForSqlServerAsync(masterConnectionString);
        await DropDatabaseAsync(masterConnectionString, UpgradeDatabaseName);
        await CreateDatabaseAsync(masterConnectionString, UpgradeDatabaseName);

        var dbBuilder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = UpgradeDatabaseName
        };
        string connectionString = dbBuilder.ConnectionString;

        try
        {
            string legacySql = await File.ReadAllTextAsync(ResolveLegacyBootstrapPath());
            await using (SqlConnection connection = new(connectionString))
            {
                await connection.OpenAsync();
                await ExecuteBatchesAsync(connection, legacySql);
            }

            await SeedLegacyDataAsync(connectionString);

            Assert.False(await ObjectExistsAsync(connectionString, "dbo", "UserIdentity"));
            Assert.False(await ObjectExistsAsync(connectionString, "dbo", "PlatformRole"));
            Assert.False(await ObjectExistsAsync(connectionString, "org", "StaffMembership"));
            Assert.True(await ObjectExistsAsync(connectionString, "org", "OrganizationUser"));
            Assert.True(await ColumnIsNullableAsync(connectionString, "dbo", "User", "PasswordHash") == false);

            DacpacDeployer.UpgradeExisting(masterConnectionString, UpgradeDatabaseName);

            Assert.True(await ObjectExistsAsync(connectionString, "dbo", "UserIdentity"));
            Assert.True(await ObjectExistsAsync(connectionString, "dbo", "PlatformRole"));
            Assert.True(await ObjectExistsAsync(connectionString, "org", "StaffMembership"));
            Assert.True(await ColumnIsNullableAsync(connectionString, "dbo", "User", "PasswordHash"));

            // Post-deploy PlatformRoles seed should map GlobalRoleId → PlatformUserRole.
            await using (SqlConnection verify = new(connectionString))
            {
                await verify.OpenAsync();
                int platformAssignments = await verify.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT_BIG(1)
                    FROM [dbo].[PlatformUserRole]
                    WHERE [UserId] = @UserId;
                    """,
                    new { UserId = LegacyAdminUserId });

                Assert.True(
                    platformAssignments >= 1,
                    "Legacy global admin must receive a PlatformUserRole after upgrade.");
            }

            string cutoverSql = await File.ReadAllTextAsync(ResolveCutoverScriptPath());
            await using (SqlConnection cutover = new(connectionString))
            {
                await cutover.OpenAsync();
                await cutover.ExecuteAsync(cutoverSql);
                await cutover.ExecuteAsync(cutoverSql);
            }

            Assert.True(await IsIsolationPolicyEnabledAsync(connectionString));

            await using SqlConnection after = new(connectionString);
            await after.OpenAsync();
            await after.ExecuteAsync(
                """
                ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                    WITH (STATE = OFF);
                """);

            try
            {
                int memberships = await after.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT_BIG(1)
                    FROM [org].[StaffMembership]
                    WHERE [UserId] IN (@AdminId, @MemberId)
                      AND [DeletedAt] IS NULL;
                    """,
                    new { AdminId = LegacyAdminUserId, MemberId = LegacyMemberUserId });

                Assert.Equal(3, memberships);

                int staffRoles = await after.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT_BIG(1)
                    FROM [org].[StaffRole] AS staffRole
                    INNER JOIN [org].[StaffMembership] AS membership
                        ON membership.[Id] = staffRole.[StaffMembershipId]
                    WHERE membership.[UserId] IN (@AdminId, @MemberId);
                    """,
                    new { AdminId = LegacyAdminUserId, MemberId = LegacyMemberUserId });

                Assert.Equal(3, staffRoles);

                int orphans = await after.ExecuteScalarAsync<int>(
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

                string? passwordHash = await after.ExecuteScalarAsync<string>(
                    """
                    SELECT [PasswordHash]
                    FROM [dbo].[User]
                    WHERE [Id] = @UserId;
                    """,
                    new { UserId = LegacyAdminUserId });

                Assert.Equal("legacy-hash", passwordHash);
            }
            finally
            {
                await after.ExecuteAsync(
                    """
                    ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                        WITH (STATE = ON);
                    """);
            }

            Assert.True(await IsIsolationPolicyEnabledAsync(connectionString));
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await DropDatabaseAsync(masterConnectionString, UpgradeDatabaseName);
        }
    }

    private static async Task SeedLegacyDataAsync(string connectionString)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            """
            INSERT INTO [dbo].[Role]
                ([Id], [Name], [Key], [Description], [Scope], [CreatedAt], [UpdatedAt])
            VALUES
                (@RoleId, N'Global administrator', N'global_admin', N'Legacy', 1, @Now, @Now);

            INSERT INTO [dbo].[Organization]
                ([Id], [Name], [Key], [CreatedAt], [UpdatedAt])
            VALUES
                (@OrgA, N'Legacy Org A', N'legacy-org-a', @Now, @Now),
                (@OrgB, N'Legacy Org B', N'legacy-org-b', @Now, @Now);

            INSERT INTO [dbo].[User]
                ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
                 [DisplayName], [PasswordHash], [PasswordSalt], [GlobalRoleId],
                 [CreatedAt], [UpdatedAt])
            VALUES
                (@AdminId, N'legacy.admin@test', N'LEGACY.ADMIN@TEST',
                 N'legacy.admin@test', N'LEGACY.ADMIN@TEST', N'Legacy Admin',
                 N'legacy-hash', N'legacy-salt', @RoleId, @Now, @Now),
                (@MemberId, N'legacy.member@test', N'LEGACY.MEMBER@TEST',
                 N'legacy.member@test', N'LEGACY.MEMBER@TEST', N'Legacy Member',
                 N'member-hash', N'member-salt', NULL, @Now, @Now);
            """,
            new
            {
                RoleId = GlobalAdminRoleId,
                OrgA = OrgAId,
                OrgB = OrgBId,
                AdminId = LegacyAdminUserId,
                MemberId = LegacyMemberUserId,
                Now = now
            });

        await SetOrganizationContextAsync(connection, OrgAId);
        await connection.ExecuteAsync(
            """
            INSERT INTO [org].[Role]
                ([Id], [OrganizationId], [Name], [Key], [IsSystemRole], [IsActive],
                 [CreatedAt], [UpdatedAt])
            VALUES
                (@RoleId, @OrgId, N'Administrator', N'administrator', 1, 1, @Now, @Now);

            INSERT INTO [org].[OrganizationUser]
                ([OrganizationId], [UserId], [RoleId], [IsActive], [CreatedAt], [UpdatedAt])
            VALUES
                (@OrgId, @AdminId, @RoleId, 1, @Now, @Now),
                (@OrgId, @MemberId, @RoleId, 1, @Now, @Now);
            """,
            new
            {
                RoleId = OrgARoleId,
                OrgId = OrgAId,
                AdminId = LegacyAdminUserId,
                MemberId = LegacyMemberUserId,
                Now = now
            });

        await SetOrganizationContextAsync(connection, OrgBId);
        await connection.ExecuteAsync(
            """
            INSERT INTO [org].[Role]
                ([Id], [OrganizationId], [Name], [Key], [IsSystemRole], [IsActive],
                 [CreatedAt], [UpdatedAt])
            VALUES
                (@RoleId, @OrgId, N'Administrator', N'administrator', 1, 1, @Now, @Now);

            INSERT INTO [org].[OrganizationUser]
                ([OrganizationId], [UserId], [RoleId], [IsActive], [CreatedAt], [UpdatedAt])
            VALUES
                (@OrgId, @AdminId, @RoleId, 1, @Now, @Now);
            """,
            new
            {
                RoleId = OrgBRoleId,
                OrgId = OrgBId,
                AdminId = LegacyAdminUserId,
                Now = now
            });

        await SetOrganizationContextAsync(connection, null);
    }

    private static async Task SetOrganizationContextAsync(
        SqlConnection connection,
        Guid? organizationId)
    {
        await using SqlCommand command = new(
            "EXEC sp_set_session_context @key = N'OrganizationId', @value = @OrganizationId;",
            connection);
        SqlParameter parameter = command.Parameters.Add(
            "@OrganizationId",
            System.Data.SqlDbType.UniqueIdentifier);
        parameter.Value = organizationId.HasValue ? organizationId.Value : DBNull.Value;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteBatchesAsync(SqlConnection connection, string script)
    {
        foreach (string batch in script.Split(["\nGO\r", "\nGO\n", "\nGO"], StringSplitOptions.RemoveEmptyEntries))
        {
            string sql = batch.Trim();
            if (sql.Length == 0 || sql.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await connection.ExecuteAsync(sql);
        }
    }

    private static async Task<bool> ObjectExistsAsync(
        string connectionString,
        string schema,
        string name)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT_BIG(1)
            FROM sys.objects
            WHERE object_id = OBJECT_ID(@FullName);
            """,
            new { FullName = $"{schema}.{name}" }) > 0;
    }

    private static async Task<bool> ColumnIsNullableAsync(
        string connectionString,
        string schema,
        string table,
        string column)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT CAST(c.is_nullable AS BIT)
            FROM sys.columns AS c
            INNER JOIN sys.tables AS t ON t.object_id = c.object_id
            INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE s.name = @Schema
              AND t.name = @Table
              AND c.name = @Column;
            """,
            new { Schema = schema, Table = table, Column = column });
    }

    private static async Task<bool> IsIsolationPolicyEnabledAsync(string connectionString)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT CAST(is_enabled AS BIT)
            FROM sys.security_policies
            WHERE name = N'OrganizationIsolationPolicy'
              AND schema_id = SCHEMA_ID(N'org');
            """);
    }

    private static async Task CreateDatabaseAsync(string masterConnectionString, string databaseName)
    {
        await using SqlConnection connection = new(masterConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            $"CREATE DATABASE [{databaseName}];");
    }

    private static async Task DropDatabaseAsync(string masterConnectionString, string databaseName)
    {
        await using SqlConnection connection = new(masterConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            $"""
            IF DB_ID(N'{databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END
            """);
    }

    private static async Task WaitForSqlServerAsync(string masterConnectionString)
    {
        for (int attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                await using SqlConnection connection = new(masterConnectionString);
                await connection.OpenAsync();
                return;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new InvalidOperationException(
            "SQL Server was not reachable for the legacy upgrade test.");
    }

    private static string ResolveLegacyBootstrapPath()
    {
        string fromOutput = Path.Combine(
            AppContext.BaseDirectory,
            "Legacy",
            "LegacySchemaBootstrap.sql");

        if (File.Exists(fromOutput))
        {
            return fromOutput;
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "Legacy",
                "LegacySchemaBootstrap.sql");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("LegacySchemaBootstrap.sql was not found.");
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

        throw new FileNotFoundException("SCRUM-81-StaffMembership-Cutover.sql was not found.");
    }
}
