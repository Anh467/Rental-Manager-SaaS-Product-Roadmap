-- Membership of a global user in an organization, with exactly one role in that
-- organization. A user may belong to many organizations through many rows here.
CREATE TABLE [org].[OrganizationUser]
(
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_OrganizationUser_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,

    CONSTRAINT [PK_OrganizationUser]
        PRIMARY KEY CLUSTERED ([OrganizationId], [UserId]),

    CONSTRAINT [FK_OrganizationUser_Role]
        FOREIGN KEY ([OrganizationId], [RoleId])
        REFERENCES [org].[Role] ([OrganizationId], [Id]),

    CONSTRAINT [FK_OrganizationUser_User]
        FOREIGN KEY ([UserId])
        REFERENCES [dbo].[User] ([Id])
);
GO

CREATE INDEX [IX_OrganizationUser_User]
    ON [org].[OrganizationUser] ([UserId], [IsActive])
    INCLUDE ([RoleId]);
GO
