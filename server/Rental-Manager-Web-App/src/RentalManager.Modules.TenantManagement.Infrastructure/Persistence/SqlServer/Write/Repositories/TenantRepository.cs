using System.Data.Common;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Domain.Tenants;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Write.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly IWriteDbSession _session;

    public TenantRepository(IWriteDbSession session)
    {
        _session = session;
    }

    public async Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM [org].[Tenant]
                WHERE [Code] = @Code
            ) THEN 1 ELSE 0 END;
            """;

        DbCommand command = await CreateCommandAsync(sql, cancellationToken);
        AddParameter(command, "@Code", TenantRules.NormalizeCode(code));

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }

    public async Task<Tenant?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                [TenantId],
                [Code],
                [Name],
                [Status],
                [CreatedAt],
                [UpdatedAt]
            FROM [org].[Tenant]
            WHERE [TenantId] = @TenantId;
            """;

        DbCommand command = await CreateCommandAsync(sql, cancellationToken);
        AddParameter(command, "@TenantId", tenantId);

        await using DbDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        int updatedAtOrdinal = reader.GetOrdinal("UpdatedAt");

        return Tenant.Rehydrate(
            reader.GetGuid(reader.GetOrdinal("TenantId")),
            reader.GetString(reader.GetOrdinal("Code")),
            reader.GetString(reader.GetOrdinal("Name")),
            (TenantStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            reader.IsDBNull(updatedAtOrdinal)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(updatedAtOrdinal));
    }

    public async Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO [org].[Tenant]
            (
                [TenantId],
                [Code],
                [Name],
                [Status],
                [CreatedAt],
                [UpdatedAt]
            )
            VALUES
            (
                @TenantId,
                @Code,
                @Name,
                @Status,
                @CreatedAt,
                @UpdatedAt
            );
            """;

        DbCommand command = await CreateCommandAsync(sql, cancellationToken);
        AddTenantParameters(command, tenant);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [org].[Tenant]
            SET
                [Name] = @Name,
                [Status] = @Status,
                [UpdatedAt] = @UpdatedAt
            WHERE [TenantId] = @TenantId;
            """;

        DbCommand command = await CreateCommandAsync(sql, cancellationToken);
        AddParameter(command, "@TenantId", tenant.Id);
        AddParameter(command, "@Name", tenant.Name);
        AddParameter(command, "@Status", (int)tenant.Status);
        AddParameter(command, "@UpdatedAt", tenant.UpdatedAt);

        int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
        {
            throw new KeyNotFoundException(
                $"Tenant '{tenant.Id}' was not found while updating.");
        }
    }

    private async Task<DbCommand> CreateCommandAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        await _session.OpenAsync(cancellationToken);

        DbCommand command = _session.Connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _session.Transaction;
        return command;
    }

    private static void AddTenantParameters(
        DbCommand command,
        Tenant tenant)
    {
        AddParameter(command, "@TenantId", tenant.Id);
        AddParameter(command, "@Code", tenant.Code);
        AddParameter(command, "@Name", tenant.Name);
        AddParameter(command, "@Status", (int)tenant.Status);
        AddParameter(command, "@CreatedAt", tenant.CreatedAt);
        AddParameter(command, "@UpdatedAt", tenant.UpdatedAt);
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
