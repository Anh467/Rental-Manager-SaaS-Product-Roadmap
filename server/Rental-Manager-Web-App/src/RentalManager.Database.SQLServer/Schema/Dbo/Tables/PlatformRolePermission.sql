-- Grants a PlatformRole only dbo.Permission rows whose Scope is Platform.
CREATE TABLE [dbo].[PlatformRolePermission]
(
    [PlatformRoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] INT NOT NULL,

    CONSTRAINT [PK_PlatformRolePermission]
        PRIMARY KEY CLUSTERED ([PlatformRoleId], [PermissionId]),

    CONSTRAINT [FK_PlatformRolePermission_PlatformRole]
        FOREIGN KEY ([PlatformRoleId])
        REFERENCES [dbo].[PlatformRole] ([Id]),

    CONSTRAINT [FK_PlatformRolePermission_Permission]
        FOREIGN KEY ([PermissionId])
        REFERENCES [dbo].[Permission] ([Id])
);
GO
