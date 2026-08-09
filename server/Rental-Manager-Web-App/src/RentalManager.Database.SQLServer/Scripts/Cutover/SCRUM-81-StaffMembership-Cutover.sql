-- SCRUM-81 cutover: OrganizationUser → StaffMembership + StaffRole
--
-- Idempotent. Safe to re-run. Fails loudly when post-conditions are not met.
-- Not executed by PostDeployment (clean publish seeds Staff* directly).
-- Upgrade path: deploy DACPAC (creates Staff* tables), then run this script,
-- then validate counts before allowing runtime traffic on the new model.
--
-- Residual [org].[OrganizationUser] is intentionally retained for one release.
-- Runtime must not read or write it after this cutover.
--
-- RLS: OrganizationUser / StaffMembership / StaffRole are filtered by
-- [org].[OrganizationIsolationPolicy]. SQL Server applies RLS to dbo/sysadmin
-- as well, so this elevated upgrade script must briefly disable that policy,
-- migrate while it can see every row, then ALWAYS re-enable it before exit.

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PolicyWasDisabled BIT = 0;

BEGIN TRY
    -- Disable outside the data transaction so a rollback cannot leave the
    -- policy OFF, and so the migration can see every OrganizationUser row.
    ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
        WITH (STATE = OFF);
    SET @PolicyWasDisabled = 1;

    BEGIN TRANSACTION;

    DECLARE @SourceCount INT =
    (
        SELECT COUNT_BIG(1)
        FROM [org].[OrganizationUser]
    );

    DECLARE @ExistingMembershipCount INT =
    (
        SELECT COUNT_BIG(1)
        FROM [org].[StaffMembership]
        WHERE [DeletedAt] IS NULL
    );

    -- Already cut over: nothing to do when StaffMembership already covers sources.
    IF @SourceCount > 0
       AND @ExistingMembershipCount >= @SourceCount
       AND NOT EXISTS
       (
           SELECT 1
           FROM [org].[OrganizationUser] AS source
           WHERE NOT EXISTS
           (
               SELECT 1
               FROM [org].[StaffMembership] AS membership
               WHERE membership.[OrganizationId] = source.[OrganizationId]
                 AND membership.[UserId] = source.[UserId]
                 AND membership.[DeletedAt] IS NULL
           )
       )
    BEGIN
        COMMIT TRANSACTION;

        BEGIN TRY
            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = ON);
            SET @PolicyWasDisabled = 0;
        END TRY
        BEGIN CATCH
            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = ON);
            SET @PolicyWasDisabled = 0;
            THROW;
        END CATCH;

        RETURN;
    END;

    -- Map each OrganizationUser to a StaffMembership (Active if IsActive else Inactive).
    ;WITH SourceRows AS
    (
        SELECT
            source.[OrganizationId],
            source.[UserId],
            source.[RoleId],
            source.[IsActive],
            source.[CreatedAt],
            source.[UpdatedAt],
            -- Deterministic id so re-runs stay stable when a row was partially written.
            CONVERT(
                UNIQUEIDENTIFIER,
                HASHBYTES(
                    'MD5',
                    CONVERT(VARBINARY(16), source.[OrganizationId]) +
                    CONVERT(VARBINARY(16), source.[UserId]))) AS [StaffMembershipId]
        FROM [org].[OrganizationUser] AS source
    )
    MERGE [org].[StaffMembership] AS target
    USING
    (
        SELECT DISTINCT
            [StaffMembershipId],
            [OrganizationId],
            [UserId],
            CASE WHEN [IsActive] = 1 THEN CONVERT(TINYINT, 2) ELSE CONVERT(TINYINT, 3) END AS [Status],
            [CreatedAt],
            [UpdatedAt]
        FROM SourceRows
    ) AS source
    ON target.[OrganizationId] = source.[OrganizationId]
       AND target.[UserId] = source.[UserId]
       AND target.[DeletedAt] IS NULL
    WHEN MATCHED THEN
        UPDATE SET
            [Status] = source.[Status],
            [UpdatedAt] = source.[UpdatedAt]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            [Id],
            [OrganizationId],
            [UserId],
            [Status],
            [CreatedAt],
            [UpdatedAt],
            [DeletedAt]
        )
        VALUES
        (
            source.[StaffMembershipId],
            source.[OrganizationId],
            source.[UserId],
            source.[Status],
            source.[CreatedAt],
            source.[UpdatedAt],
            NULL
        );

    -- Map old single RoleId onto StaffRole.
    ;WITH SourceRoles AS
    (
        SELECT
            membership.[Id] AS [StaffMembershipId],
            membership.[OrganizationId],
            source.[RoleId],
            source.[CreatedAt],
            source.[UpdatedAt],
            CONVERT(
                UNIQUEIDENTIFIER,
                HASHBYTES(
                    'MD5',
                    CONVERT(VARBINARY(16), membership.[Id]) +
                    CONVERT(VARBINARY(16), source.[RoleId]))) AS [StaffRoleId]
        FROM [org].[OrganizationUser] AS source
        INNER JOIN [org].[StaffMembership] AS membership
            ON membership.[OrganizationId] = source.[OrganizationId]
           AND membership.[UserId] = source.[UserId]
           AND membership.[DeletedAt] IS NULL
    )
    MERGE [org].[StaffRole] AS target
    USING SourceRoles AS source
    ON target.[StaffMembershipId] = source.[StaffMembershipId]
       AND target.[RoleId] = source.[RoleId]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            [Id],
            [OrganizationId],
            [StaffMembershipId],
            [RoleId],
            [CreatedAt],
            [UpdatedAt]
        )
        VALUES
        (
            source.[StaffRoleId],
            source.[OrganizationId],
            source.[StaffMembershipId],
            source.[RoleId],
            source.[CreatedAt],
            source.[UpdatedAt]
        );

    -- Post-condition checks.
    DECLARE @MembershipCount INT =
    (
        SELECT COUNT_BIG(1)
        FROM [org].[StaffMembership]
        WHERE [DeletedAt] IS NULL
    );

    DECLARE @MissingMembership INT =
    (
        SELECT COUNT_BIG(1)
        FROM [org].[OrganizationUser] AS source
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [org].[StaffMembership] AS membership
            WHERE membership.[OrganizationId] = source.[OrganizationId]
              AND membership.[UserId] = source.[UserId]
              AND membership.[DeletedAt] IS NULL
        )
    );

    DECLARE @MissingStaffRole INT =
    (
        SELECT COUNT_BIG(1)
        FROM [org].[OrganizationUser] AS source
        INNER JOIN [org].[StaffMembership] AS membership
            ON membership.[OrganizationId] = source.[OrganizationId]
           AND membership.[UserId] = source.[UserId]
           AND membership.[DeletedAt] IS NULL
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [org].[StaffRole] AS staffRole
            WHERE staffRole.[StaffMembershipId] = membership.[Id]
              AND staffRole.[RoleId] = source.[RoleId]
        )
    );

    DECLARE @OrphanStaffRole INT =
    (
        SELECT COUNT_BIG(1)
        FROM [org].[StaffRole] AS staffRole
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [org].[StaffMembership] AS membership
            WHERE membership.[Id] = staffRole.[StaffMembershipId]
              AND membership.[OrganizationId] = staffRole.[OrganizationId]
        )
    );

    DECLARE @CrossOrgStaffRole INT =
    (
        SELECT COUNT_BIG(1)
        FROM [org].[StaffRole] AS staffRole
        INNER JOIN [org].[StaffMembership] AS membership
            ON membership.[Id] = staffRole.[StaffMembershipId]
        WHERE membership.[OrganizationId] <> staffRole.[OrganizationId]
    );

    IF @SourceCount > 0 AND @MembershipCount = 0
    BEGIN
        THROW 50081,
            N'SCRUM-81 cutover failed: source OrganizationUser rows exist but zero StaffMembership rows were visible after migration (RLS filtering?).',
            1;
    END;

    IF @MissingMembership > 0
       OR @MissingStaffRole > 0
       OR @OrphanStaffRole > 0
       OR @CrossOrgStaffRole > 0
       OR @MembershipCount < @SourceCount
    BEGIN
        DECLARE @Message NVARCHAR(400) = CONCAT(
            N'SCRUM-81 cutover failed. source=',
            @SourceCount,
            N' memberships=',
            @MembershipCount,
            N' missingMembership=',
            @MissingMembership,
            N' missingStaffRole=',
            @MissingStaffRole,
            N' orphanStaffRole=',
            @OrphanStaffRole,
            N' crossOrgStaffRole=',
            @CrossOrgStaffRole);

        THROW 50081, @Message, 1;
    END;

    COMMIT TRANSACTION;

    BEGIN TRY
        ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
            WITH (STATE = ON);
        SET @PolicyWasDisabled = 0;
    END TRY
    BEGIN CATCH
        ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
            WITH (STATE = ON);
        SET @PolicyWasDisabled = 0;
        THROW;
    END CATCH;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    IF @PolicyWasDisabled = 1
    BEGIN
        BEGIN TRY
            ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                WITH (STATE = ON);
        END TRY
        BEGIN CATCH
            -- Prefer surfacing the original migration failure; still attempt
            -- a second re-enable so runtime never stays with RLS off.
            BEGIN TRY
                ALTER SECURITY POLICY [org].[OrganizationIsolationPolicy]
                    WITH (STATE = ON);
            END TRY
            BEGIN CATCH
            END CATCH;
        END CATCH;
    END;

    THROW;
END CATCH;
