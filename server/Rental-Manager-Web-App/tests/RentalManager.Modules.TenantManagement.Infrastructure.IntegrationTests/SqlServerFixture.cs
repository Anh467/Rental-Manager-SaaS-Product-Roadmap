using Microsoft.Data.SqlClient;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

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
        await CreateDatabaseAsync();

        var testBuilder = new SqlConnectionStringBuilder(
            _masterConnectionString)
        {
            InitialCatalog = TestDatabaseName
        };

        ConnectionString = testBuilder.ConnectionString;
        await CreateSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(
            _masterConnectionString);

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

    public async Task ResetFieldsAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using SqlCommand command = new(
            "DELETE FROM [dbo].[Field];",
            connection);

        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new SqlConnection(
            _masterConnectionString);

        await connection.OpenAsync();

        string sql = $"""
            IF DB_ID(N'{TestDatabaseName}') IS NULL
            BEGIN
                CREATE DATABASE [{TestDatabaseName}];
            END;
            """;

        await using SqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateSchemaAsync()
    {
        const string sql = """
            CREATE TABLE [dbo].[FieldType]
            (
                [Id] INT NOT NULL,
                [Name] NVARCHAR(256) NOT NULL,
                [Key] NVARCHAR(256) NOT NULL,
                [Description] NVARCHAR(1028) NULL,

                CONSTRAINT [PK_FieldType]
                    PRIMARY KEY CLUSTERED ([Id]),

                CONSTRAINT [UQ_FieldType_Key]
                    UNIQUE ([Key])
            );

            CREATE TABLE [dbo].[Field]
            (
                [Id] UNIQUEIDENTIFIER NOT NULL,
                [Name] NVARCHAR(256) NOT NULL,
                [Key] NVARCHAR(256) NOT NULL,
                [Description] NVARCHAR(1028) NULL,
                [FieldTypeId] INT NOT NULL,
                [UpdatedAt] DATETIMEOFFSET NOT NULL,
                [CreatedAt] DATETIMEOFFSET NOT NULL,
                [DeletedAt] DATETIMEOFFSET NULL,

                CONSTRAINT [PK_Field]
                    PRIMARY KEY CLUSTERED ([Id]),

                CONSTRAINT [FK_Field_FieldType]
                    FOREIGN KEY ([FieldTypeId])
                    REFERENCES [dbo].[FieldType] ([Id])
            );

            INSERT INTO [dbo].[FieldType]
            (
                [Id],
                [Name],
                [Key],
                [Description]
            )
            VALUES
                (1, N'Text', N'TEXT', N'Free-form text value.'),
                (2, N'Number', N'NUMBER', N'Integer or decimal numeric value.'),
                (3, N'Date', N'DATE', N'Date or date-time value.'),
                (4, N'Boolean', N'BOOLEAN', N'True or false value.'),
                (5, N'Selection', N'SELECTION', N'Value selected from predefined options.');
            """;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

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
