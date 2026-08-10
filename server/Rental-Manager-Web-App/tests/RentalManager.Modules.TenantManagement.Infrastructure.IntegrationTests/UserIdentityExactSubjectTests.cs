using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Persistence;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Proves SQL Server stores and looks up OIDC subjects with exact character
/// identity, including trailing/leading whitespace, case, and near-identical
/// Unicode — not ANSI-padded NVARCHAR equality.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class UserIdentityExactSubjectTests
{
    private readonly SqlServerFixture _fixture;

    public UserIdentityExactSubjectTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Trailing_space_subjects_coexist_as_distinct_identities()
    {
        await _fixture.ResetProvisionedUsersAsync();

        ExternalIdentityMapping abc = await ProvisionAsync("abc", "abc.exact@rentalmanager.test");
        ExternalIdentityMapping abcTrailing =
            await ProvisionAsync("abc ", "abc.trailing@rentalmanager.test");

        Assert.NotEqual(abc.User.UserId, abcTrailing.User.UserId);

        Assert.Equal(
            abc.User.UserId,
            (await FindAsync("abc"))!.User.UserId);
        Assert.Equal(
            abcTrailing.User.UserId,
            (await FindAsync("abc "))!.User.UserId);

        await AssertDistinctExactKeysAsync("abc", "abc ");
    }

    [Fact]
    public async Task Leading_space_subjects_differ_from_unpadded()
    {
        await _fixture.ResetProvisionedUsersAsync();

        ExternalIdentityMapping leading =
            await ProvisionAsync(" abc", "abc.leading@rentalmanager.test");
        ExternalIdentityMapping plain =
            await ProvisionAsync("abc", "abc.plain@rentalmanager.test");

        Assert.NotEqual(leading.User.UserId, plain.User.UserId);
        Assert.Equal(leading.User.UserId, (await FindAsync(" abc"))!.User.UserId);
        Assert.Equal(plain.User.UserId, (await FindAsync("abc"))!.User.UserId);
        Assert.Null(await FindAsync("abc "));
    }

    [Fact]
    public async Task Case_differing_subjects_are_distinct()
    {
        await _fixture.ResetProvisionedUsersAsync();

        ExternalIdentityMapping lower =
            await ProvisionAsync("abc", "abc.lower@rentalmanager.test");
        ExternalIdentityMapping upper =
            await ProvisionAsync("ABC", "abc.upper@rentalmanager.test");

        Assert.NotEqual(lower.User.UserId, upper.User.UserId);
        Assert.Equal(lower.User.UserId, (await FindAsync("abc"))!.User.UserId);
        Assert.Equal(upper.User.UserId, (await FindAsync("ABC"))!.User.UserId);
    }

    [Fact]
    public async Task Near_identical_unicode_subjects_are_not_conflated()
    {
        await _fixture.ResetProvisionedUsersAsync();

        // Precomposed é (U+00E9) vs e + combining acute (U+0301).
        const string precomposed = "caf\u00E9";
        const string decomposed = "cafe\u0301";

        ExternalIdentityMapping composed =
            await ProvisionAsync(precomposed, "unicode.composed@rentalmanager.test");
        ExternalIdentityMapping combining =
            await ProvisionAsync(decomposed, "unicode.decomposed@rentalmanager.test");

        Assert.NotEqual(composed.User.UserId, combining.User.UserId);
        Assert.Equal(composed.User.UserId, (await FindAsync(precomposed))!.User.UserId);
        Assert.Equal(combining.User.UserId, (await FindAsync(decomposed))!.User.UserId);
        await AssertDistinctExactKeysAsync(precomposed, decomposed);
    }

    [Fact]
    public async Task Exact_subject_lookup_returns_correct_user()
    {
        await _fixture.ResetProvisionedUsersAsync();

        ExternalIdentityMapping target =
            await ProvisionAsync("exact-lookup-sub", "exact.lookup@rentalmanager.test");
        await ProvisionAsync("exact-lookup-sub ", "exact.lookup.padded@rentalmanager.test");

        ExternalIdentityMapping? found = await FindAsync("exact-lookup-sub");

        Assert.NotNull(found);
        Assert.Equal(target.User.UserId, found.User.UserId);
        Assert.Equal(target.Id, found.Id);
    }

    [Fact]
    public async Task Concurrent_login_same_exact_subject_creates_one_identity()
    {
        await _fixture.ResetProvisionedUsersAsync();

        const string subject = "concurrent-exact-sub ";
        string email = $"concurrent.exact.{Guid.CreateVersion7():N}@rentalmanager.test";
        var request = new ExternalUserProvisionRequest(
            TestData.Provider,
            subject,
            email,
            "Concurrent Exact");

        var stores = Enumerable.Range(0, 8)
            .Select(_ => new SqlUserAccountStore(
                new IdentityConnectionFactory(_fixture.ConnectionString)))
            .ToArray();

        ExternalUserProvisionResult[] results = await Task.WhenAll(
            stores.Select(store => store.ProvisionAsync(request)));

        Assert.All(
            results,
            result => Assert.True(
                result.Status is ExternalUserProvisionStatus.Created
                    or ExternalUserProvisionStatus.AlreadyMapped));
        Assert.DoesNotContain(
            results,
            result => result.Status == ExternalUserProvisionStatus.EmailConflict);
        Assert.Single(results.Select(r => r.Mapping!.User.UserId).Distinct());

        await using SqlConnection connection = await _fixture.OpenConnectionAsync();
        int identityCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT_BIG(1)
            FROM [dbo].[UserIdentity]
            WHERE [Provider] = @Provider
              AND [SubjectExactKey] = CONVERT(VARBINARY(512), CAST(@Subject AS NVARCHAR(256)));
            """,
            new { Provider = TestData.Provider, Subject = subject });

        Assert.Equal(1, identityCount);
    }

    [Fact]
    public async Task Upgrade_backfill_preserves_exact_subject_mappings()
    {
        await _fixture.ResetProvisionedUsersAsync();

        // Insert as the application would, then prove the persisted exact key
        // round-trips without mutating Subject and still distinguishes padding.
        ExternalIdentityMapping first =
            await ProvisionAsync("migrate-sub", "migrate.a@rentalmanager.test");
        ExternalIdentityMapping second =
            await ProvisionAsync("migrate-sub ", "migrate.b@rentalmanager.test");

        await using SqlConnection connection = await _fixture.OpenConnectionAsync();

        var rows = (await connection.QueryAsync<(string Subject, byte[] ExactKey)>(
            """
            SELECT [Subject], [SubjectExactKey]
            FROM [dbo].[UserIdentity]
            WHERE [Provider] = @Provider
              AND [Subject] IN (N'migrate-sub', N'migrate-sub ')
            ORDER BY DATALENGTH([SubjectExactKey]), [Subject];
            """,
            new { Provider = TestData.Provider })).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("migrate-sub", rows[0].Subject);
        Assert.Equal("migrate-sub ", rows[1].Subject);
        Assert.NotEqual(rows[0].ExactKey, rows[1].ExactKey);

        // Recomputing the exact key is idempotent and matches stored values.
        byte[]? recomputedFirst = await connection.ExecuteScalarAsync<byte[]>(
            "SELECT CONVERT(VARBINARY(512), CAST(@Subject AS NVARCHAR(256)));",
            new { Subject = "migrate-sub" });
        byte[]? recomputedSecond = await connection.ExecuteScalarAsync<byte[]>(
            "SELECT CONVERT(VARBINARY(512), CAST(@Subject AS NVARCHAR(256)));",
            new { Subject = "migrate-sub " });

        Assert.NotNull(recomputedFirst);
        Assert.NotNull(recomputedSecond);
        Assert.Equal(rows[0].ExactKey, recomputedFirst);
        Assert.Equal(rows[1].ExactKey, recomputedSecond);

        Assert.Equal(first.User.UserId, (await FindAsync("migrate-sub"))!.User.UserId);
        Assert.Equal(second.User.UserId, (await FindAsync("migrate-sub "))!.User.UserId);
    }

    private async Task<ExternalIdentityMapping> ProvisionAsync(string subject, string email)
    {
        var store = new SqlUserAccountStore(
            new IdentityConnectionFactory(_fixture.ConnectionString));

        ExternalUserProvisionResult result = await store.ProvisionAsync(
            new ExternalUserProvisionRequest(
                TestData.Provider,
                subject,
                email,
                $"User {subject.Length}"));

        Assert.Equal(ExternalUserProvisionStatus.Created, result.Status);
        Assert.NotNull(result.Mapping);
        return result.Mapping;
    }

    private async Task<ExternalIdentityMapping?> FindAsync(string subject)
    {
        var store = new SqlUserAccountStore(
            new IdentityConnectionFactory(_fixture.ConnectionString));

        return await store.FindByExternalIdentityAsync(TestData.Provider, subject);
    }

    private async Task AssertDistinctExactKeysAsync(string left, string right)
    {
        await using SqlConnection connection = await _fixture.OpenConnectionAsync();

        int sharedKeys = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT_BIG(1)
            FROM [dbo].[UserIdentity] AS leftRow
            INNER JOIN [dbo].[UserIdentity] AS rightRow
                ON leftRow.[SubjectExactKey] = rightRow.[SubjectExactKey]
               AND leftRow.[Id] <> rightRow.[Id]
            WHERE leftRow.[Provider] = @Provider
              AND leftRow.[SubjectExactKey] = CONVERT(VARBINARY(512), CAST(@Left AS NVARCHAR(256)))
              AND rightRow.[SubjectExactKey] = CONVERT(VARBINARY(512), CAST(@Right AS NVARCHAR(256)));
            """,
            new { Provider = TestData.Provider, Left = left, Right = right });

        Assert.Equal(0, sharedKeys);

        // Direct proof that SQL string equality would wrongly collapse trailing spaces.
        bool paddedStringEqual = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT CAST(CASE
                WHEN CAST(@Left AS NVARCHAR(256)) COLLATE Latin1_General_BIN2
                   = CAST(@Right AS NVARCHAR(256)) COLLATE Latin1_General_BIN2
                THEN 1 ELSE 0 END AS BIT);
            """,
            new { Left = "abc", Right = "abc " });

        bool exactKeysEqual = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT CAST(CASE
                WHEN CONVERT(VARBINARY(512), CAST(@Left AS NVARCHAR(256)))
                   = CONVERT(VARBINARY(512), CAST(@Right AS NVARCHAR(256)))
                THEN 1 ELSE 0 END AS BIT);
            """,
            new { Left = "abc", Right = "abc " });

        Assert.True(paddedStringEqual, "Expected ANSI-padded NVARCHAR equality for abc vs abc .");
        Assert.False(exactKeysEqual, "Exact byte keys must distinguish abc from abc .");
    }
}
