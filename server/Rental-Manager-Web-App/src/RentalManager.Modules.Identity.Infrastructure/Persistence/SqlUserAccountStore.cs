using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Dapper-backed platform user and external identity persistence. Provisioning
/// is the only place a user row is created, and it always writes the user and
/// its identity mapping inside one transaction, so a failure can never leave a
/// user whose email is taken but who can never sign in.
/// </summary>
public sealed class SqlUserAccountStore(IIdentityConnectionFactory connectionFactory)
    : IUserAccountStore
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<AuthenticatedIdentity?> FindActiveByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        UserRow? row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(
                IdentitySqlStatements.FindActiveUserById,
                new { UserId = userId },
                cancellationToken: cancellationToken));

        return row?.ToIdentity();
    }

    public async Task<ExternalIdentityMapping?> FindByExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await FindMappingAsync(
            connection,
            transaction: null,
            provider,
            subject,
            cancellationToken);
    }

    public async Task<bool> IsEmailInUseAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                IdentitySqlStatements.IsEmailInUse,
                new { NormalizedEmail = NormalizeEmail(email) },
                cancellationToken: cancellationToken));
    }

    public async Task<ExternalUserProvisionResult> ProvisionAsync(
        ExternalUserProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);

        string provider = NormalizeProvider(request.Provider);
        string email = request.Email.Trim();
        Guid userId = Guid.CreateVersion7();
        Guid userIdentityId = Guid.CreateVersion7();

        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    IdentitySqlStatements.InsertUser,
                    new
                    {
                        Id = userId,
                        UserName = email,
                        NormalizedUserName = NormalizeEmail(email),
                        Email = email,
                        NormalizedEmail = NormalizeEmail(email),
                        DisplayName = request.DisplayName,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        request.GlobalRoleId,
                        Now = DateTimeOffset.UtcNow
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    IdentitySqlStatements.InsertUserIdentity,
                    new
                    {
                        Id = userIdentityId,
                        UserId = userId,
                        Provider = provider,
                        request.Subject,
                        Now = DateTimeOffset.UtcNow
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException exception)
            when (exception.Number is UniqueIndexViolation or UniqueConstraintViolation)
        {
            await transaction.RollbackAsync(cancellationToken);

            if (IsViolationOf(exception, IdentitySqlStatements.UserIdentityUniqueIndexName))
            {
                // Another request provisioned the same subject first, so the
                // mapping it created is the correct answer for this caller too.
                ExternalIdentityMapping? existing = await FindMappingAsync(
                    connection,
                    transaction: null,
                    provider,
                    request.Subject,
                    cancellationToken);

                return existing is null
                    ? ExternalUserProvisionResult.EmailConflict()
                    : ExternalUserProvisionResult.AlreadyMapped(existing);
            }

            if (IsViolationOf(exception, IdentitySqlStatements.UserEmailUniqueIndexName) ||
                IsViolationOf(exception, IdentitySqlStatements.UserNameUniqueIndexName))
            {
                return ExternalUserProvisionResult.EmailConflict();
            }

            throw;
        }

        ExternalIdentityMapping created = await FindMappingAsync(
            connection,
            transaction: null,
            provider,
            request.Subject,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The identity mapping was committed but could not be read back.");

        return ExternalUserProvisionResult.Created(created);
    }

    public async Task RecordLoginAsync(
        Guid userIdentityId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                IdentitySqlStatements.RecordLogin,
                new { Id = userIdentityId, OccurredAt = occurredAt },
                cancellationToken: cancellationToken));
    }

    private static async Task<ExternalIdentityMapping?> FindMappingAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string provider,
        string subject,
        CancellationToken cancellationToken)
    {
        MappingRow? row = await connection.QuerySingleOrDefaultAsync<MappingRow>(
            new CommandDefinition(
                IdentitySqlStatements.FindMappingByProviderAndSubject,
                new { Provider = NormalizeProvider(provider), Subject = subject },
                transaction,
                cancellationToken: cancellationToken));

        return row is null
            ? null
            : new ExternalIdentityMapping(row.UserIdentityId, row.Provider, row.ToIdentity());
    }

    /// <summary>
    /// SQL Server does not expose the violated index as a separate field, so the
    /// index name is matched inside the message. Only names this project owns are
    /// matched, and nothing from the exception is ever returned to a caller.
    /// </summary>
    private static bool IsViolationOf(SqlException exception, string indexName) =>
        exception.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeEmail(string email) =>
        email.Trim().ToUpperInvariant();

    private static string NormalizeProvider(string provider) =>
        provider.Trim().ToLowerInvariant();

    private class UserRow
    {
        public Guid UserId { get; init; }

        public string? Email { get; init; }

        public string? DisplayName { get; init; }

        public string? SecurityStamp { get; init; }

        public Guid? GlobalRoleId { get; init; }

        public bool IsActive { get; init; }

        public AuthenticatedIdentity ToIdentity() =>
            new(
                UserId,
                Email ?? string.Empty,
                DisplayName ?? string.Empty,
                SecurityStamp ?? string.Empty,
                GlobalRoleId,
                IsActive);
    }

    private sealed class MappingRow : UserRow
    {
        public Guid UserIdentityId { get; init; }

        public string Provider { get; init; } = string.Empty;
    }
}
