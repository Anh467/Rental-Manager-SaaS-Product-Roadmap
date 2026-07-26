-- Join table granting a global permission to an organization role.
CREATE TABLE [org].[RolePermission]
(
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] INT NOT NULL,

    CONSTRAINT [PK_RolePermission]
        PRIMARY KEY CLUSTERED ([OrganizationId], [RoleId], [PermissionId]),

    CONSTRAINT [FK_RolePermission_Role]
        FOREIGN KEY ([OrganizationId], [RoleId])
        REFERENCES [org].[Role] ([OrganizationId], [Id]),

    CONSTRAINT [FK_RolePermission_Permission]
        FOREIGN KEY ([PermissionId])
        REFERENCES [dbo].[Permission] ([Id])
);
