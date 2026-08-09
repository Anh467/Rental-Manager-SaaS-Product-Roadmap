using System.Data;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.Identity.Infrastructure.Persistence;

namespace RentalManager.Modules.Identity.Infrastructure.Identity;

public sealed class DapperUserStore :
    IUserStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>,
    IUserSecurityStampStore<ApplicationUser>,
    IUserLockoutStore<ApplicationUser>
{
    private const string IdentityHashPrefix = "AQAAAA";

    private readonly IIdentityConnectionFactory _connectionFactory;
    private readonly IdentityErrorDescriber _errorDescriber;

    public DapperUserStore(
        IIdentityConnectionFactory connectionFactory,
        IdentityErrorDescriber? errorDescriber = null)
    {
        _connectionFactory = connectionFactory;
        _errorDescriber = errorDescriber ?? new IdentityErrorDescriber();
    }

    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(
        ApplicationUser user,
        string? userName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(
        ApplicationUser user,
        string? normalizedName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.CreateVersion7();
        }

        if (string.IsNullOrWhiteSpace(user.SecurityStamp))
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
        }

        if (string.IsNullOrWhiteSpace(user.ConcurrencyStamp))
        {
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
        }

        user.CreatedAt = now;
        user.UpdatedAt = now;
        NormalizeLegacySalt(user);

        try
        {
            await using SqlConnection connection =
                await _connectionFactory.OpenConnectionAsync(cancellationToken);

            byte[]? rowVersion = await connection.QuerySingleOrDefaultAsync<byte[]>(
                new CommandDefinition(
                    IdentitySqlStatements.InsertUser,
                    CreateWriteParameters(user, expectedConcurrencyStamp: null),
                    cancellationToken: cancellationToken));

            if (rowVersion is null)
            {
                return IdentityResult.Failed(_errorDescriber.DefaultError());
            }

            user.RowVersion = rowVersion;
            return IdentityResult.Success;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return MapUniqueConstraintFailure(exception);
        }
    }

    public async Task<IdentityResult> UpdateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string expectedConcurrencyStamp = user.ConcurrencyStamp
            ?? throw new InvalidOperationException(
                "ConcurrencyStamp is required for updates.");

        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        NormalizeLegacySalt(user);

        try
        {
            await using SqlConnection connection =
                await _connectionFactory.OpenConnectionAsync(cancellationToken);

            UserUpdateResult? result =
                await connection.QuerySingleOrDefaultAsync<UserUpdateResult>(
                    new CommandDefinition(
                        IdentitySqlStatements.UpdateUser,
                        CreateWriteParameters(user, expectedConcurrencyStamp),
                        cancellationToken: cancellationToken));

            if (result is null || result.AffectedRows == 0)
            {
                return IdentityResult.Failed(_errorDescriber.ConcurrencyFailure());
            }

            if (result.RowVersion is not null)
            {
                user.RowVersion = result.RowVersion;
            }

            return IdentityResult.Success;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return MapUniqueConstraintFailure(exception);
        }
    }

    public async Task<IdentityResult> DeleteAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string expectedConcurrencyStamp = user.ConcurrencyStamp
            ?? throw new InvalidOperationException(
                "ConcurrencyStamp is required for deletes.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        user.DeletedAt = now;
        user.UpdatedAt = now;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        int affected = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                IdentitySqlStatements.SoftDeleteUser,
                new
                {
                    user.Id,
                    user.DeletedAt,
                    user.UpdatedAt,
                    user.ConcurrencyStamp,
                    ExpectedConcurrencyStamp = expectedConcurrencyStamp
                },
                cancellationToken: cancellationToken));

        return affected == 0
            ? IdentityResult.Failed(_errorDescriber.ConcurrencyFailure())
            : IdentityResult.Success;
    }

    public async Task<ApplicationUser?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Guid.TryParse(userId, out Guid id))
        {
            return null;
        }

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            new CommandDefinition(
                IdentitySqlStatements.FindById,
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    public async Task<ApplicationUser?> FindByNameAsync(
        string normalizedUserName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedUserName);

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            new CommandDefinition(
                IdentitySqlStatements.FindByNormalizedUserName,
                new { NormalizedUserName = normalizedUserName },
                cancellationToken: cancellationToken));
    }

    public Task SetPasswordHashAsync(
        ApplicationUser user,
        string? passwordHash,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.PasswordHash = passwordHash;
        if (IsIdentityHash(passwordHash))
        {
            user.PasswordSalt = null;
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    public Task SetEmailAsync(
        ApplicationUser user,
        string? email,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(
        ApplicationUser user,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(
            new CommandDefinition(
                IdentitySqlStatements.FindByNormalizedEmail,
                new { NormalizedEmail = normalizedEmail },
                cancellationToken: cancellationToken));
    }

    public Task<string?> GetNormalizedEmailAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(
        ApplicationUser user,
        string? normalizedEmail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(
        ApplicationUser user,
        string stamp,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.SecurityStamp);
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.LockoutEnd);
    }

    public Task SetLockoutEndDateAsync(
        ApplicationUser user,
        DateTimeOffset? lockoutEnd,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<bool> GetLockoutEnabledAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task SetLockoutEnabledAsync(
        ApplicationUser user,
        bool enabled,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    private IdentityResult MapUniqueConstraintFailure(SqlException exception)
    {
        string message = exception.Message;
        if (message.Contains("NormalizedEmail", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("UX_User_NormalizedEmail", StringComparison.OrdinalIgnoreCase))
        {
            return IdentityResult.Failed(_errorDescriber.DuplicateEmail(string.Empty));
        }

        if (message.Contains("NormalizedUserName", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("UX_User_NormalizedUserName", StringComparison.OrdinalIgnoreCase))
        {
            return IdentityResult.Failed(_errorDescriber.DuplicateUserName(string.Empty));
        }

        return IdentityResult.Failed(_errorDescriber.DefaultError());
    }

    private static void NormalizeLegacySalt(ApplicationUser user)
    {
        if (IsIdentityHash(user.PasswordHash))
        {
            user.PasswordSalt = null;
        }
    }

    private static bool IsIdentityHash(string? passwordHash) =>
        !string.IsNullOrEmpty(passwordHash) &&
        passwordHash.StartsWith(IdentityHashPrefix, StringComparison.Ordinal);

    private static object CreateWriteParameters(
        ApplicationUser user,
        string? expectedConcurrencyStamp)
    {
        return new
        {
            user.Id,
            user.UserName,
            user.NormalizedUserName,
            user.Email,
            user.NormalizedEmail,
            user.EmailConfirmed,
            user.DisplayName,
            user.PasswordHash,
            user.PasswordSalt,
            user.SecurityStamp,
            user.ConcurrencyStamp,
            user.LockoutEnd,
            user.LockoutEnabled,
            user.AccessFailedCount,
            user.GlobalRoleId,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            user.DeletedAt,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp
        };
    }

    private sealed class UserUpdateResult
    {
        public int AffectedRows { get; init; }

        public byte[]? RowVersion { get; init; }
    }
}
