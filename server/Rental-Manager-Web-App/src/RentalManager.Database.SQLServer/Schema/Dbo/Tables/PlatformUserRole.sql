-- Assigns a PlatformRole to a platform User. This is the durable Platform RBAC
-- assignment path; dbo.User.GlobalRoleId is legacy and must not remain the
-- permanent source of platform permissions.
CREATE TABLE [dbo].[PlatformUserRole]
(
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [PlatformRoleId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL
        CONSTRAINT [DF_PlatformUserRole_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_PlatformUserRole]
        PRIMARY KEY CLUSTERED ([UserId], [PlatformRoleId]),

    CONSTRAINT [FK_PlatformUserRole_User]
        FOREIGN KEY ([UserId])
        REFERENCES [dbo].[User] ([Id]),

    CONSTRAINT [FK_PlatformUserRole_PlatformRole]
        FOREIGN KEY ([PlatformRoleId])
        REFERENCES [dbo].[PlatformRole] ([Id])
);
GO
