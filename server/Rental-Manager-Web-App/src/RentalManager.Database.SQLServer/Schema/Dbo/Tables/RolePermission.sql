CREATE TABLE [dbo].[RolePermission]
(
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] INT NOT NULL,

    CONSTRAINT [PK_RolePermission]
        PRIMARY KEY CLUSTERED ([RoleId], [PermissionId]),

    CONSTRAINT [FK_RolePermission_Role]
        FOREIGN KEY ([RoleId])
        REFERENCES [dbo].[Role] ([Id]),

    CONSTRAINT [FK_RolePermission_Permission]
        FOREIGN KEY ([PermissionId])
        REFERENCES [dbo].[Permission] ([Id])
);
