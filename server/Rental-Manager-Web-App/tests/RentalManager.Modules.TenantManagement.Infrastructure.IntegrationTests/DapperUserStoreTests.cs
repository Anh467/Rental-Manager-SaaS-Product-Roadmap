using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.Identity.Infrastructure.Identity;
using RentalManager.Modules.Identity.Infrastructure.Persistence;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class DapperUserStoreTests
{
    private readonly SqlServerFixture _fixture;

    public DapperUserStoreTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_find_by_normalized_email_and_password_roundtrip()
    {
        DapperUserStore store = CreateStore();
        Guid id = Guid.CreateVersion7();
        string email = $"store-{id:N}@example.com";
        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = "Store User",
            PasswordHash = "AQAAAAIAAYagAAAAAPlaceholderHashValue==",
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true
        };

        IdentityResult create = await store.CreateAsync(user, CancellationToken.None);
        Assert.True(create.Succeeded);

        ApplicationUser? found =
            await store.FindByEmailAsync(email.ToUpperInvariant(), CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(id, found.Id);

        await store.SetPasswordHashAsync(found, "AQAAAAIAAYagAAAAATestHash==", CancellationToken.None);
        IdentityResult update = await store.UpdateAsync(found, CancellationToken.None);
        Assert.True(update.Succeeded);

        ApplicationUser? reloaded =
            await store.FindByIdAsync(id.ToString(), CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal("AQAAAAIAAYagAAAAATestHash==", reloaded.PasswordHash);
    }

    [Fact]
    public async Task Duplicate_normalized_email_returns_identity_error()
    {
        DapperUserStore store = CreateStore();
        string email = TestData.Users.AdministratorAEmail.ToUpperInvariant();

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = $"dup-{Guid.NewGuid():N}@example.com",
            NormalizedUserName = $"DUP-{Guid.NewGuid():N}@EXAMPLE.COM",
            Email = $"dup-{Guid.NewGuid():N}@example.com",
            NormalizedEmail = email,
            EmailConfirmed = true,
            DisplayName = "Duplicate Email User",
            PasswordHash = "AQAAAAIAAYagAAAAAPlaceholderHashValue==",
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true
        };

        IdentityResult result = await store.CreateAsync(user, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Security_stamp_access_failed_and_lockout_persist()
    {
        DapperUserStore store = CreateStore();
        Guid id = Guid.CreateVersion7();
        string email = $"lockout-{id:N}@example.com";
        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = "Lockout User",
            PasswordHash = "AQAAAAIAAYagAAAAAPlaceholderHashValue==",
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true
        };

        Assert.True((await store.CreateAsync(user, CancellationToken.None)).Succeeded);

        string stamp = Guid.NewGuid().ToString();
        await store.SetSecurityStampAsync(user, stamp, CancellationToken.None);
        await store.ResetAccessFailedCountAsync(user, CancellationToken.None);
        await store.IncrementAccessFailedCountAsync(user, CancellationToken.None);
        await store.IncrementAccessFailedCountAsync(user, CancellationToken.None);
        await store.IncrementAccessFailedCountAsync(user, CancellationToken.None);
        DateTimeOffset lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        await store.SetLockoutEndDateAsync(user, lockoutEnd, CancellationToken.None);

        IdentityResult update = await store.UpdateAsync(user, CancellationToken.None);
        Assert.True(update.Succeeded);

        ApplicationUser? reloaded =
            await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(stamp, reloaded.SecurityStamp);
        Assert.Equal(3, reloaded.AccessFailedCount);
        Assert.NotNull(reloaded.LockoutEnd);
    }

    [Fact]
    public async Task Concurrency_conflict_returns_concurrency_failure()
    {
        DapperUserStore store = CreateStore();
        Guid id = Guid.CreateVersion7();
        string email = $"concurrency-{id:N}@example.com";
        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = "Concurrency User",
            PasswordHash = "AQAAAAIAAYagAAAAAPlaceholderHashValue==",
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true
        };

        Assert.True((await store.CreateAsync(user, CancellationToken.None)).Succeeded);

        ApplicationUser first = await store.FindByIdAsync(id.ToString(), CancellationToken.None)
            ?? throw new InvalidOperationException("User vanished.");
        ApplicationUser second = await store.FindByIdAsync(id.ToString(), CancellationToken.None)
            ?? throw new InvalidOperationException("User vanished.");

        first.DisplayName = "Concurrency winner";
        Assert.True((await store.UpdateAsync(first, CancellationToken.None)).Succeeded);

        second.DisplayName = "Concurrency loser";
        IdentityResult conflict = await store.UpdateAsync(second, CancellationToken.None);
        Assert.False(conflict.Succeeded);
        Assert.Contains(conflict.Errors, error => error.Code == "ConcurrencyFailure");
    }

    [Fact]
    public async Task Cancellation_is_honored()
    {
        DapperUserStore store = CreateStore();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.FindByEmailAsync("ANY@EXAMPLE.COM", cts.Token));
    }

    private DapperUserStore CreateStore() =>
        new(new IdentityConnectionFactory(_fixture.ConnectionString));
}
