SET NOCOUNT ON;

-- Canonical PLATFORM_SUPER_ADMIN role (Confluence 15.1).
DECLARE @SuperAdminRoleId UNIQUEIDENTIFIER = '6F2A8B1C-9D4E-4F3A-A7B8-1C2D3E4F5A6B';
DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

MERGE [dbo].[PlatformRole] AS target
USING (SELECT
           @SuperAdminRoleId AS [Id],
           N'PLATFORM_SUPER_ADMIN' AS [Key],
           N'Platform super administrator' AS [Name],
           N'All platform permissions.' AS [Description]) AS source
ON target.[Id] = source.[Id]
WHEN MATCHED THEN UPDATE SET
    [Key] = source.[Key],
    [Name] = source.[Name],
    [Description] = source.[Description],
    [IsActive] = 1,
    [UpdatedAt] = @Now,
    [DeletedAt] = NULL
WHEN NOT MATCHED THEN INSERT
    ([Id], [Key], [Name], [Description], [IsActive], [CreatedAt], [UpdatedAt], [DeletedAt])
VALUES
    (source.[Id], source.[Key], source.[Name], source.[Description], 1, @Now, @Now, NULL);

-- Grant every active Platform-scoped permission.
MERGE [dbo].[PlatformRolePermission] AS target
USING (
    SELECT @SuperAdminRoleId AS [PlatformRoleId], [Id] AS [PermissionId]
    FROM [dbo].[Permission]
    WHERE [Scope] = 1
      AND [IsActive] = 1
) AS source
ON target.[PlatformRoleId] = source.[PlatformRoleId]
   AND target.[PermissionId] = source.[PermissionId]
WHEN NOT MATCHED THEN INSERT ([PlatformRoleId], [PermissionId])
VALUES (source.[PlatformRoleId], source.[PermissionId]);

-- Migrate legacy GlobalRoleId / dbo.Role global_admin assignments into
-- PlatformUserRole without removing GlobalRoleId (cutover keeps access).
DECLARE @GlobalAdminRoleId UNIQUEIDENTIFIER = '0F4FA0E8-2397-4CC3-B3A1-02189F0DCC34';

INSERT INTO [dbo].[PlatformUserRole] ([UserId], [PlatformRoleId], [CreatedAt])
SELECT
    u.[Id],
    @SuperAdminRoleId,
    @Now
FROM [dbo].[User] AS u
WHERE u.[DeletedAt] IS NULL
  AND u.[GlobalRoleId] = @GlobalAdminRoleId
  AND NOT EXISTS
  (
      SELECT 1
      FROM [dbo].[PlatformUserRole] AS assignment
      WHERE assignment.[UserId] = u.[Id]
        AND assignment.[PlatformRoleId] = @SuperAdminRoleId
  );
