-- Many-to-many assignment of organization roles to a staff membership.
-- Composite foreign keys include OrganizationId so a role or membership from
-- another organization cannot be referenced.
CREATE TABLE [org].[StaffRole]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [StaffMembershipId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,

    CONSTRAINT [PK_StaffRole]
        PRIMARY KEY NONCLUSTERED ([Id]),

    CONSTRAINT [UQ_StaffRole_Membership_Role]
        UNIQUE ([StaffMembershipId], [RoleId]),

    CONSTRAINT [FK_StaffRole_StaffMembership]
        FOREIGN KEY ([OrganizationId], [StaffMembershipId])
        REFERENCES [org].[StaffMembership] ([OrganizationId], [Id]),

    CONSTRAINT [FK_StaffRole_Role]
        FOREIGN KEY ([OrganizationId], [RoleId])
        REFERENCES [org].[Role] ([OrganizationId], [Id])
);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_StaffRole_OrganizationId]
    ON [org].[StaffRole] ([OrganizationId], [Id]);
GO

CREATE INDEX [IX_StaffRole_Membership]
    ON [org].[StaffRole] ([StaffMembershipId])
    INCLUDE ([RoleId], [OrganizationId]);
GO
