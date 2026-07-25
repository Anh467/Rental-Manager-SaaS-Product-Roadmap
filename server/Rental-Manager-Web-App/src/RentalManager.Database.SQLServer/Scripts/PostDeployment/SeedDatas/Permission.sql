SET NOCOUNT ON;

DECLARE @Permissions TABLE
(
    [Id] INT NOT NULL,
    [Code] NVARCHAR(128) NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [Module] NVARCHAR(64) NOT NULL
);

INSERT INTO @Permissions ([Id], [Code], [Name], [Description], [Module])
VALUES
    (1, N'field.view',   N'View fields',   N'Read organization field definitions.',   N'TenantManagement'),
    (2, N'field.add',    N'Add fields',    N'Create organization field definitions.', N'TenantManagement'),
    (3, N'field.edit',   N'Edit fields',   N'Update organization field definitions.', N'TenantManagement'),
    (4, N'field.delete', N'Delete fields', N'Remove organization field definitions.', N'TenantManagement');

UPDATE target
SET
    target.[Code] = source.[Code],
    target.[Name] = source.[Name],
    target.[Description] = source.[Description],
    target.[Module] = source.[Module]
FROM [dbo].[Permission] AS target
INNER JOIN @Permissions AS source
    ON source.[Id] = target.[Id];

INSERT INTO [dbo].[Permission]
(
    [Id],
    [Code],
    [Name],
    [Description],
    [Module]
)
SELECT
    source.[Id],
    source.[Code],
    source.[Name],
    source.[Description],
    source.[Module]
FROM @Permissions AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Permission] AS target
    WHERE target.[Id] = source.[Id]
);
