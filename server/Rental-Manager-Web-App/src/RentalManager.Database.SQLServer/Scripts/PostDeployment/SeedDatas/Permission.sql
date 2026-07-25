SET NOCOUNT ON;

DECLARE @Permissions TABLE
(
    [Id] INT NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [Module] NVARCHAR(64) NOT NULL
);

INSERT INTO @Permissions ([Id], [Name], [Key], [Description], [Module])
VALUES
    (1, N'View fields',   N'field_view',   N'Read organization field definitions.',   N'TenantManagement'),
    (2, N'Add fields',    N'field_add',    N'Create organization field definitions.', N'TenantManagement'),
    (3, N'Edit fields',   N'field_edit',   N'Update organization field definitions.', N'TenantManagement'),
    (4, N'Delete fields', N'field_delete', N'Remove organization field definitions.', N'TenantManagement');

UPDATE target
SET
    target.[Name] = source.[Name],
    target.[Key] = source.[Key],
    target.[Description] = source.[Description],
    target.[Module] = source.[Module]
FROM [dbo].[Permission] AS target
INNER JOIN @Permissions AS source
    ON source.[Id] = target.[Id];

INSERT INTO [dbo].[Permission]
(
    [Id],
    [Name],
    [Key],
    [Description],
    [Module]
)
SELECT
    source.[Id],
    source.[Name],
    source.[Key],
    source.[Description],
    source.[Module]
FROM @Permissions AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Permission] AS target
    WHERE target.[Id] = source.[Id]
);
