namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

internal static class IdentitySqlStatements
{
    public const string UserSelectColumns = """
        [Id],
        [UserName],
        [NormalizedUserName],
        [Email],
        [NormalizedEmail],
        [EmailConfirmed],
        [DisplayName],
        [PasswordHash],
        [PasswordSalt],
        [SecurityStamp],
        [ConcurrencyStamp],
        [PhoneNumber],
        [PhoneNumberConfirmed],
        [TwoFactorEnabled],
        [LockoutEnd],
        [LockoutEnabled],
        [AccessFailedCount],
        [GlobalRoleId],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [DeletedAt],
        [RowVersion]
        """;

    // PhoneNumber / TwoFactor columns are not persisted; selected as constants so
    // Dapper can hydrate IdentityUser properties with safe defaults.
    public const string UserSelectProjection = """
        [Id],
        [UserName],
        [NormalizedUserName],
        [Email],
        [NormalizedEmail],
        [EmailConfirmed],
        [DisplayName],
        [PasswordHash],
        [PasswordSalt],
        [SecurityStamp],
        [ConcurrencyStamp],
        CAST(NULL AS NVARCHAR(50)) AS [PhoneNumber],
        CAST(0 AS BIT) AS [PhoneNumberConfirmed],
        CAST(0 AS BIT) AS [TwoFactorEnabled],
        [LockoutEnd],
        [LockoutEnabled],
        [AccessFailedCount],
        [GlobalRoleId],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [DeletedAt],
        [RowVersion]
        """;

    public const string FindById = $"""
        SELECT {UserSelectProjection}
        FROM [dbo].[User]
        WHERE [Id] = @Id
          AND [DeletedAt] IS NULL;
        """;

    public const string FindByNormalizedUserName = $"""
        SELECT {UserSelectProjection}
        FROM [dbo].[User]
        WHERE [NormalizedUserName] = @NormalizedUserName
          AND [DeletedAt] IS NULL;
        """;

    public const string FindByNormalizedEmail = $"""
        SELECT {UserSelectProjection}
        FROM [dbo].[User]
        WHERE [NormalizedEmail] = @NormalizedEmail
          AND [DeletedAt] IS NULL;
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
            [PasswordHash],
            [PasswordSalt],
            [SecurityStamp],
            [ConcurrencyStamp],
            [LockoutEnd],
            [LockoutEnabled],
            [AccessFailedCount],
            [GlobalRoleId],
            [IsActive],
            [CreatedAt],
            [UpdatedAt],
            [DeletedAt]
        )
        VALUES
        (
            @Id,
            @UserName,
            @NormalizedUserName,
            @Email,
            @NormalizedEmail,
            @EmailConfirmed,
            @DisplayName,
            @PasswordHash,
            @PasswordSalt,
            @SecurityStamp,
            @ConcurrencyStamp,
            @LockoutEnd,
            @LockoutEnabled,
            @AccessFailedCount,
            @GlobalRoleId,
            @IsActive,
            @CreatedAt,
            @UpdatedAt,
            @DeletedAt
        );

        SELECT [RowVersion]
        FROM [dbo].[User]
        WHERE [Id] = @Id;
        """;

    public const string UpdateUser = """
        UPDATE [dbo].[User]
        SET
            [UserName] = @UserName,
            [NormalizedUserName] = @NormalizedUserName,
            [Email] = @Email,
            [NormalizedEmail] = @NormalizedEmail,
            [EmailConfirmed] = @EmailConfirmed,
            [DisplayName] = @DisplayName,
            [PasswordHash] = @PasswordHash,
            [PasswordSalt] = @PasswordSalt,
            [SecurityStamp] = @SecurityStamp,
            [ConcurrencyStamp] = @ConcurrencyStamp,
            [LockoutEnd] = @LockoutEnd,
            [LockoutEnabled] = @LockoutEnabled,
            [AccessFailedCount] = @AccessFailedCount,
            [GlobalRoleId] = @GlobalRoleId,
            [IsActive] = @IsActive,
            [UpdatedAt] = @UpdatedAt,
            [DeletedAt] = @DeletedAt
        WHERE [Id] = @Id
          AND [ConcurrencyStamp] = @ExpectedConcurrencyStamp
          AND [DeletedAt] IS NULL;

        DECLARE @AffectedRows INT = @@ROWCOUNT;

        SELECT
            @AffectedRows AS [AffectedRows],
            (
                SELECT [RowVersion]
                FROM [dbo].[User]
                WHERE [Id] = @Id
            ) AS [RowVersion];
        """;

    public const string SoftDeleteUser = """
        UPDATE [dbo].[User]
        SET
            [DeletedAt] = @DeletedAt,
            [UpdatedAt] = @UpdatedAt,
            [ConcurrencyStamp] = @ConcurrencyStamp
        WHERE [Id] = @Id
          AND [ConcurrencyStamp] = @ExpectedConcurrencyStamp
          AND [DeletedAt] IS NULL;

        SELECT @@ROWCOUNT;
        """;

    public const string ListActiveOrganizations = """
        EXEC [dbo].[usp_ListActiveOrganizationMembershipsForUser] @UserId = @UserId;
        """;
}
