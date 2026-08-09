-- RETIRED (SCRUM-81): residual table retained for one-release upgrade safety.
-- Runtime application code must not read or write this table. Source of truth
-- is [org].[StaffMembership] + [org].[StaffRole]. See Scripts/Cutover/
-- SCRUM-81-StaffMembership-Cutover.sql.
--
-- Historical model: membership of a global user in an organization, with
-- exactly one role in that organization.
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
