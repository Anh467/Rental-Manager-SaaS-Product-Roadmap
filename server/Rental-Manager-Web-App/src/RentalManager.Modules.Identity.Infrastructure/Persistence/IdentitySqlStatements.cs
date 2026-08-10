namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

internal static class IdentitySqlStatements
{
    public const string UserIdentityUniqueIndexName = "UX_UserIdentity_Provider_SubjectExactKey";

    public const string UserEmailUniqueIndexName = "UX_User_NormalizedEmail";

    public const string UserNameUniqueIndexName = "UX_User_NormalizedUserName";

    /// <summary>
    /// Only what a session needs. Retired credential columns are deliberately
    /// never selected, so no runtime code path can start depending on them again.
    /// </summary>
    private const string UserColumns = """
        [Id] AS [UserId],
        [Email],
        [DisplayName],
        [SecurityStamp],
        [GlobalRoleId],
        [IsActive]
        """;

    private const string JoinedUserColumns = """
        [account].[Id] AS [UserId],
        [account].[Email],
        [account].[DisplayName],
        [account].[SecurityStamp],
        [account].[GlobalRoleId],
        [account].[IsActive]
        """;

    public const string FindActiveUserById = $"""
        SELECT {UserColumns}
        FROM [dbo].[User]
        WHERE [Id] = @UserId
          AND [DeletedAt] IS NULL
          AND [IsActive] = 1;
        """;

    /// <summary>
    /// Lookup uses the persisted UTF-16LE byte key so subjects that differ only
    /// by trailing/leading whitespace, case, or Unicode code points are distinct
    /// despite SQL Server string padding. The stored Subject is returned so the
    /// caller can verify ordinal equality before trusting the row. The user is
    /// returned even when inactive so the caller can answer with the inactive
    /// message instead of a misleading "not found".
    /// </summary>
    public const string FindMappingByProviderAndSubject = $"""
        SELECT
            [mapping].[Id] AS [UserIdentityId],
            [mapping].[Provider],
            [mapping].[Subject],
            {JoinedUserColumns}
        FROM [dbo].[UserIdentity] AS [mapping]
        INNER JOIN [dbo].[User] AS [account]
            ON [account].[Id] = [mapping].[UserId]
        WHERE [mapping].[Provider] = @Provider
          AND [mapping].[SubjectExactKey] = CONVERT(VARBINARY(512), CAST(@Subject AS NVARCHAR(256)))
          AND [account].[DeletedAt] IS NULL;
        """;

    public const string IsEmailInUse = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1
            FROM [dbo].[User]
            WHERE [NormalizedEmail] = @NormalizedEmail
              AND [DeletedAt] IS NULL
        ) THEN 1 ELSE 0 END AS BIT);
        """;

    public const string InsertUser = """
        INSERT INTO [dbo].[User]
        (
            [Id],
            [UserName],
            [NormalizedUserName],
            [Email],
            [NormalizedEmail],
            [EmailConfirmed],
            [DisplayName],
            [SecurityStamp],
            [ConcurrencyStamp],
            [LockoutEnabled],
            [AccessFailedCount],
            [GlobalRoleId],
            [IsActive],
            [CreatedAt],
            [UpdatedAt]
        )
        VALUES
        (
            @Id,
            @UserName,
            @NormalizedUserName,
            @Email,
            @NormalizedEmail,
            1,
            @DisplayName,
            @SecurityStamp,
            @ConcurrencyStamp,
            0,
            0,
            @GlobalRoleId,
            1,
            @Now,
            @Now
        );
        """;

    public const string InsertUserIdentity = """
        INSERT INTO [dbo].[UserIdentity]
        (
            [Id],
            [UserId],
            [Provider],
            [Subject],
            [CreatedAt],
            [UpdatedAt],
            [LastLoginAt]
        )
        VALUES
        (
            @Id,
            @UserId,
            @Provider,
            @Subject,
            @Now,
            @Now,
            NULL
        );
        """;

    public const string RecordLogin = """
        UPDATE [dbo].[UserIdentity]
        SET
            [LastLoginAt] = @OccurredAt,
            [UpdatedAt] = @OccurredAt
        WHERE [Id] = @Id;
        """;

    public const string ListActiveOrganizations = """
        EXEC [dbo].[usp_ListActiveOrganizationMembershipsForUser] @UserId = @UserId;
        """;
}
