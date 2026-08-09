SET NOCOUNT ON;

-- Scope: 1 = Platform, 2 = Organization (Confluence 15.1).
DECLARE @Permissions TABLE
(
    [Id] INT NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [Module] NVARCHAR(64) NOT NULL,
    [Scope] INT NOT NULL
);

INSERT INTO @Permissions ([Id], [Name], [Key], [Description], [Module], [Scope])
VALUES
    -- Existing TenantManagement keys (underscore form already in production).
    (1, N'View fields',   N'field_view',   N'Read organization field definitions.',   N'TenantManagement', 2),
    (2, N'Add fields',    N'field_add',    N'Create organization field definitions.', N'TenantManagement', 2),
    (3, N'Edit fields',   N'field_edit',   N'Update organization field definitions.', N'TenantManagement', 2),
    (4, N'Delete fields', N'field_delete', N'Remove organization field definitions.', N'TenantManagement', 2),
    (5, N'View global fields', N'global_field_view', N'Read global field definitions.', N'TenantManagement', 1),
    (6, N'Add global fields', N'global_field_add', N'Create global field definitions.', N'TenantManagement', 1),
    (7, N'Edit global fields', N'global_field_edit', N'Update global field definitions.', N'TenantManagement', 1),
    (8, N'Delete global fields', N'global_field_delete', N'Remove global field definitions.', N'TenantManagement', 1),

    -- Platform permissions from Confluence 15.1 (dotted keys).
    (100, N'View platform config', N'platform.config.view', N'Read platform configuration.', N'Platform', 1),
    (101, N'Add platform config', N'platform.config.add', N'Create platform configuration.', N'Platform', 1),
    (102, N'Edit platform config', N'platform.config.edit', N'Update platform configuration.', N'Platform', 1),
    (103, N'Publish platform config', N'platform.config.publish', N'Publish platform configuration.', N'Platform', 1),
    (104, N'Archive platform config', N'platform.config.archive', N'Archive platform configuration.', N'Platform', 1),
    (105, N'Override platform config', N'platform.config.override', N'Break-glass platform configuration override.', N'Platform', 1),

    (106, N'View platform organizations', N'platform.organization.view', N'Read organizations from the platform.', N'Platform', 1),
    (107, N'Add platform organizations', N'platform.organization.add', N'Create organizations from the platform.', N'Platform', 1),
    (108, N'Edit platform organizations', N'platform.organization.edit', N'Update organizations from the platform.', N'Platform', 1),
    (109, N'Deactivate platform organizations', N'platform.organization.deactivate', N'Deactivate organizations from the platform.', N'Platform', 1),

    (110, N'View provisioning', N'platform.provisioning.view', N'Read provisioning jobs.', N'Platform', 1),
    (111, N'Retry provisioning', N'platform.provisioning.retry', N'Retry provisioning jobs.', N'Platform', 1),

    (112, N'View platform users', N'platform.user.view', N'Read platform users.', N'Platform', 1),
    (113, N'Edit platform users', N'platform.user.edit', N'Update and activate platform users.', N'Platform', 1),
    (114, N'Deactivate platform users', N'platform.user.deactivate', N'Inactivate platform users.', N'Platform', 1),

    (115, N'View platform roles', N'platform.role.view', N'Read platform roles.', N'Platform', 1),
    (116, N'Add platform roles', N'platform.role.add', N'Create platform roles.', N'Platform', 1),
    (117, N'Edit platform roles', N'platform.role.edit', N'Update platform roles.', N'Platform', 1),
    (118, N'Delete platform roles', N'platform.role.delete', N'Delete platform roles.', N'Platform', 1),
    (119, N'Assign platform roles', N'platform.role.assign', N'Assign platform roles to users.', N'Platform', 1),

    (120, N'View platform permissions', N'platform.permission.view', N'Read the permission catalog.', N'Platform', 1),
    (121, N'View platform audit', N'platform.audit.view', N'Read platform audit events.', N'Platform', 1);

UPDATE target
SET
    target.[Name] = source.[Name],
    target.[Key] = source.[Key],
    target.[Description] = source.[Description],
    target.[Module] = source.[Module],
    target.[Scope] = source.[Scope]
FROM [dbo].[Permission] AS target
INNER JOIN @Permissions AS source
    ON source.[Id] = target.[Id];

INSERT INTO [dbo].[Permission]
(
    [Id],
    [Name],
    [Key],
    [Description],
    [Module],
    [Scope]
)
SELECT
    source.[Id],
    source.[Name],
    source.[Key],
    source.[Description],
    source.[Module],
    source.[Scope]
FROM @Permissions AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Permission] AS target
    WHERE target.[Id] = source.[Id]
);
