SET NOCOUNT ON;

DECLARE @RoleId UNIQUEIDENTIFIER = '0F4FA0E8-2397-4CC3-B3A1-02189F0DCC34';

MERGE [dbo].[Role] AS target
USING (SELECT @RoleId AS [Id], N'Global administrator' AS [Name],
              N'global_admin' AS [Key], N'Global field administrator.' AS [Description],
              1 AS [Scope]) AS source
ON target.[Id] = source.[Id]
WHEN MATCHED THEN UPDATE SET
    [Name] = source.[Name], [Key] = source.[Key],
    [Description] = source.[Description], [Scope] = source.[Scope],
    [UpdatedAt] = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT
    ([Id], [Name], [Key], [Description], [Scope], [CreatedAt], [UpdatedAt], [DeletedAt])
VALUES
    (source.[Id], source.[Name], source.[Key], source.[Description], source.[Scope],
     SYSUTCDATETIME(), SYSUTCDATETIME(), NULL);

MERGE [dbo].[RolePermission] AS target
USING (SELECT @RoleId AS [RoleId], [Id] AS [PermissionId]
       FROM [dbo].[Permission]
       WHERE [Key] IN (N'global_field_view', N'global_field_add', N'global_field_edit', N'global_field_delete')) AS source
ON target.[RoleId] = source.[RoleId] AND target.[PermissionId] = source.[PermissionId]
WHEN NOT MATCHED THEN INSERT ([RoleId], [PermissionId])
VALUES (source.[RoleId], source.[PermissionId]);
