using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Api.Security;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Owns the real SQL Server database these tests run against. The schema comes
/// from the DACPAC, so there is no second definition of the schema anywhere in
/// the test code.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "RentalManagerDapperTests";
    private const string DefaultMasterConnectionString =
        "Server=localhost,1433;Database=master;User Id=sa;" +
        "Password=RentalManager_Strong_Password_123!;" +
        "Encrypt=False;TrustServerCertificate=True";

    private string _masterConnectionString = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _masterConnectionString =
            Environment.GetEnvironmentVariable(
                "RENTAL_MANAGER_TEST_CONNECTION_STRING")
            ?? DefaultMasterConnectionString;

        var masterBuilder = new SqlConnectionStringBuilder(
            _masterConnectionString)
        {
            InitialCatalog = "master"
        };

        _masterConnectionString = masterBuilder.ConnectionString;

        await WaitForSqlServerAsync(_masterConnectionString);
        await DropDatabaseAsync();

        DacpacDeployer.Deploy(_masterConnectionString, TestDatabaseName);

        var testBuilder = new SqlConnectionStringBuilder(
            _masterConnectionString)
        {
            InitialCatalog = TestDatabaseName
        };

        ConnectionString = testBuilder.ConnectionString;

        await SeedTenantsAsync();
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await DropDatabaseAsync();
    }

    /// <summary>
    /// Opens a connection with no organization bound, for assertions that must
    /// observe what row level security does to an unscoped caller.
    /// </summary>
    public async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Opens a connection scoped to one organization, mirroring what
    /// <c>SqlSession</c> does in production.
    /// </summary>
    public async Task<SqlConnection> OpenConnectionAsync(Guid organizationId)
    {
        SqlConnection connection = await OpenConnectionAsync();
        await SetOrganizationContextAsync(connection, organizationId);
        return connection;
    }

    public static async Task SetOrganizationContextAsync(
        SqlConnection connection,
        Guid? organizationId)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using SqlCommand command = new(
            "EXEC sp_set_session_context @key = N'OrganizationId', " +
            "@value = @OrganizationId;",
            connection);

        SqlParameter parameter = command.Parameters.Add(
            "@OrganizationId",
            System.Data.SqlDbType.UniqueIdentifier);

        parameter.Value = organizationId.HasValue
            ? organizationId.Value
            : DBNull.Value;

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears organization-owned rows between tests. Runs without a session
    /// context, which requires disabling the isolation policy for the delete, so
    /// it is deliberately confined to this fixture.
    /// </summary>
    public async Task ResetOrgDataAsync()
    {
        await using SqlConnection connection = await OpenConnectionAsync();

        await connection.ExecuteAsync("""
            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = OFF);

            DELETE FROM [org].[AuditLog];
            DELETE FROM [org].[FieldOption];
            DELETE FROM [org].[Field];
            DELETE FROM [dbo].[Field];

            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = ON);
            """);
    }

    /// <summary>
    /// Counts rows ignoring row level security, so a test can prove a row is
    /// really absent rather than merely hidden from the caller.
    /// </summary>
    public async Task<int> CountRowsIgnoringSecurityAsync(
        string qualifiedTableName,
        string? predicate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedTableName);

        await using SqlConnection connection = await OpenConnectionAsync();

        string where = predicate is null ? string.Empty : $"WHERE {predicate}";

        return await connection.ExecuteScalarAsync<int>($"""
            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = OFF);

            DECLARE @Total INT;

            SELECT @Total = COUNT_BIG(1)
            FROM {qualifiedTableName}
            {where};

            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = ON);

            SELECT @Total;
            """);
    }

    private async Task SeedTenantsAsync()
    {
        (string hash, string salt) = PasswordHasher.Create(TestData.Password);

        await using SqlConnection connection = await OpenConnectionAsync();

        await connection.ExecuteAsync(
            """
            INSERT INTO [dbo].[Organization]
                ([Id], [Name], [NormalizedName], [CreatedAt], [UpdatedAt])
            VALUES
                (@OrganizationAId, N'Organization A', N'ORGANIZATION A', @Now, @Now),
                (@OrganizationBId, N'Organization B', N'ORGANIZATION B', @Now, @Now);

            INSERT INTO [dbo].[User]
                ([Id], [Email], [NormalizedEmail], [DisplayName],
                 [PasswordHash], [PasswordSalt], [CreatedAt], [UpdatedAt])
            VALUES
                (@AdministratorAId, @AdministratorAEmail,
                 UPPER(@AdministratorAEmail), N'Administrator A',
                 @PasswordHash, @PasswordSalt, @Now, @Now),
                (@ViewerAId, @ViewerAEmail,
                 UPPER(@ViewerAEmail), N'Viewer A',
                 @PasswordHash, @PasswordSalt, @Now, @Now),
                (@AdministratorBId, @AdministratorBEmail,
                 UPPER(@AdministratorBEmail), N'Administrator B',
                 @PasswordHash, @PasswordSalt, @Now, @Now);
            """,
            new
            {
                OrganizationAId = TestData.OrganizationA.Id,
                OrganizationBId = TestData.OrganizationB.Id,
                AdministratorAId = TestData.Users.AdministratorAId,
                TestData.Users.AdministratorAEmail,
                ViewerAId = TestData.Users.ViewerAId,
                TestData.Users.ViewerAEmail,
                AdministratorBId = TestData.Users.AdministratorBId,
                TestData.Users.AdministratorBEmail,
                PasswordHash = hash,
                PasswordSalt = salt,
                Now = DateTimeOffset.UtcNow
            });

        await SeedOrganizationRolesAsync(
            connection,
            TestData.OrganizationA.Id,
            TestData.OrganizationA.AdministratorRoleId,
            TestData.OrganizationA.ViewerRoleId,
            TestData.Users.AdministratorAId,
            TestData.Users.ViewerAId);

        await SeedOrganizationRolesAsync(
            connection,
            TestData.OrganizationB.Id,
            TestData.OrganizationB.AdministratorRoleId,
            viewerRoleId: null,
            TestData.Users.AdministratorBId,
            viewerUserId: null);
    }

    private static async Task SeedOrganizationRolesAsync(
        SqlConnection connection,
        Guid organizationId,
        Guid administratorRoleId,
        Guid? viewerRoleId,
        Guid administratorUserId,
        Guid? viewerUserId)
    {
        // Row level security blocks a write that does not agree with the session
        // context, so the seed binds the organization first.
        await SetOrganizationContextAsync(connection, organizationId);

        var parameters = new
        {
            OrganizationId = organizationId,
            AdministratorRoleId = administratorRoleId,
            ViewerRoleId = viewerRoleId,
            AdministratorUserId = administratorUserId,
            ViewerUserId = viewerUserId,
            Now = DateTimeOffset.UtcNow
        };

        await connection.ExecuteAsync(
            """
            INSERT INTO [org].[Role]
                ([Id], [OrganizationId], [Name], [NormalizedName],
                 [IsSystemRole], [CreatedAt], [UpdatedAt])
            VALUES
                (@AdministratorRoleId, @OrganizationId,
                 N'Administrator', N'ADMINISTRATOR', 1, @Now, @Now);

            INSERT INTO [org].[RolePermission]
                ([OrganizationId], [RoleId], [PermissionId])
            SELECT @OrganizationId, @AdministratorRoleId, [Id]
            FROM [dbo].[Permission];

            INSERT INTO [org].[OrganizationUser]
                ([OrganizationId], [UserId], [RoleId], [CreatedAt], [UpdatedAt])
            VALUES
                (@OrganizationId, @AdministratorUserId, @AdministratorRoleId,
                 @Now, @Now);
            """,
            parameters);

        if (viewerRoleId is null || viewerUserId is null)
        {
            return;
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO [org].[Role]
                ([Id], [OrganizationId], [Name], [NormalizedName],
                 [IsSystemRole], [CreatedAt], [UpdatedAt])
            VALUES
                (@ViewerRoleId, @OrganizationId,
                 N'Viewer', N'VIEWER', 0, @Now, @Now);

            INSERT INTO [org].[RolePermission]
                ([OrganizationId], [RoleId], [PermissionId])
            SELECT @OrganizationId, @ViewerRoleId, [Id]
            FROM [dbo].[Permission]
            WHERE [Code] = N'field.view';

            INSERT INTO [org].[OrganizationUser]
                ([OrganizationId], [UserId], [RoleId], [CreatedAt], [UpdatedAt])
            VALUES
                (@OrganizationId, @ViewerUserId, @ViewerRoleId, @Now, @Now);
            """,
            parameters);

        await SetOrganizationContextAsync(connection, organizationId: null);
    }

    private async Task DropDatabaseAsync()
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();

        string sql = $"""
            IF DB_ID(N'{TestDatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{TestDatabaseName}]
                    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

                DROP DATABASE [{TestDatabaseName}];
            END;
            """;

        await using SqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task WaitForSqlServerAsync(
        string connectionString)
    {
        const int maximumAttempts = 60;

        for (int attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(
                    connectionString);

                await connection.OpenAsync();
                return;
            }
            catch (SqlException) when (attempt < maximumAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}

[CollectionDefinition(
    SqlServerCollection.Name,
    DisableParallelization = true)]
public sealed class SqlServerCollection :
    ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server integration tests";
}
