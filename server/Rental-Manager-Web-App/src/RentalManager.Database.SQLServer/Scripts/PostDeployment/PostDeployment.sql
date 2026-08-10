:r .\SeedDatas\FieldType.sql
:r .\SeedDatas\Permission.sql
:r .\SeedDatas\GlobalAdminRole.sql
:r .\SeedDatas\PlatformRoles.sql

-- EXECUTE AS USER WITHOUT LOGIN for the membership procedure works when the
-- database owner is a sysadmin (typically sa). Prefer that over TRUSTWORTHY.
DECLARE @SetTrustworthyOff nvarchar(300) =
    N'ALTER DATABASE ' + QUOTENAME(DB_NAME()) + N' SET TRUSTWORTHY OFF;';
EXEC (@SetTrustworthyOff);

BEGIN TRY
    DECLARE @AlterAuthorization nvarchar(400) =
        N'ALTER AUTHORIZATION ON DATABASE::' + QUOTENAME(DB_NAME()) + N' TO [sa];';
    EXEC (@AlterAuthorization);
END TRY
BEGIN CATCH
    -- Hosted environments may disallow changing ownership; leave as-is.
END CATCH;

-- Backfill Identity usernames for rows that still carry empty defaults.
UPDATE [dbo].[User]
SET
    [UserName] = [Email],
    [NormalizedUserName] = [NormalizedEmail]
WHERE [DeletedAt] IS NULL
  AND (
        NULLIF(LTRIM(RTRIM([UserName])), N'') IS NULL
        OR NULLIF(LTRIM(RTRIM([NormalizedUserName])), N'') IS NULL
      );
