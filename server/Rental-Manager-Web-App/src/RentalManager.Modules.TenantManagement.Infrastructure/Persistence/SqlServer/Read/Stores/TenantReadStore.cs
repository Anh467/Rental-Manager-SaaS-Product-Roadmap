using System.Data.Common;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Core.Contracts.Tenants;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Read.Stores;

internal sealed class TenantReadStore : ITenantReadStore
{
    private readonly IReadDbConnectionFactory _connectionFactory;

    public TenantReadStore(IReadDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TenantDetails?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                [TenantId],
                [Code],
                [Name],
                [Status],
                [CreatedAt]
            FROM [org].[Tenant]
            WHERE [TenantId] = @TenantId;
            """;

        await using DbConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@TenantId", tenantId);

        await using DbDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? MapDetails(reader)
            : null;
    }

    public async Task<PagedResult<TenantListItem>> SearchAsync(
        string? keyword,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string searchSql = """
            SELECT
                [TenantId],
                [Code],
                [Name],
                [Status]
            FROM [org].[Tenant]
            WHERE
                @Keyword IS NULL OR
                [Code] LIKE '%' + @Keyword + '%' OR
                [Name] LIKE '%' + @Keyword + '%'
            ORDER BY [CreatedAt] DESC
            OFFSET @Skip ROWS
            FETCH NEXT @Take ROWS ONLY;
            """;

        const string countSql = """
            SELECT COUNT_BIG(1)
            FROM [org].[Tenant]
            WHERE
                @Keyword IS NULL OR
                [Code] LIKE '%' + @Keyword + '%' OR
                [Name] LIKE '%' + @Keyword + '%';
            """;

        await using DbConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        IReadOnlyCollection<TenantListItem> items =
            await ReadItemsAsync(
                connection,
                searchSql,
                keyword,
                (page - 1) * pageSize,
                pageSize,
                cancellationToken);

        long totalCount = await ReadCountAsync(
            connection,
            countSql,
            keyword,
            cancellationToken);

        return new PagedResult<TenantListItem>(
            items,
            page,
            pageSize,
            totalCount);
    }

    private static async Task<IReadOnlyCollection<TenantListItem>> ReadItemsAsync(
        DbConnection connection,
        string sql,
        string? keyword,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Keyword", keyword);
        AddParameter(command, "@Skip", skip);
        AddParameter(command, "@Take", take);

        var items = new List<TenantListItem>();

        await using DbDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(
                new TenantListItem(
                    reader.GetGuid(reader.GetOrdinal("TenantId")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    Convert.ToString(reader["Status"]) ?? string.Empty));
        }

        return items;
    }

    private static async Task<long> ReadCountAsync(
        DbConnection connection,
        string sql,
        string? keyword,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Keyword", keyword);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static TenantDetails MapDetails(DbDataReader reader)
    {
        return new TenantDetails(
            reader.GetGuid(reader.GetOrdinal("TenantId")),
            reader.GetString(reader.GetOrdinal("Code")),
            reader.GetString(reader.GetOrdinal("Name")),
            Convert.ToString(reader["Status"]) ?? string.Empty,
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")));
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
