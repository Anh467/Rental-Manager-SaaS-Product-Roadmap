-- Membership of a global user in one organization, with the role that decides
-- what the user may do there. A global administrator still needs a row here to
-- see organization data; there is no bypass.
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

    CONSTRAINT [FK_OrganizationUser_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id]),

    CONSTRAINT [FK_OrganizationUser_User]
        FOREIGN KEY ([UserId])
        REFERENCES [dbo].[User] ([Id]),

    CONSTRAINT [FK_OrganizationUser_Role]
        FOREIGN KEY ([RoleId])
        REFERENCES [org].[Role] ([Id])
);
