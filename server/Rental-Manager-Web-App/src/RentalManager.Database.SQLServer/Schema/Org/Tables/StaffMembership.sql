-- Staff membership of a global user in one organization.
-- Status: 1 = Pending, 2 = Active, 3 = Inactive.
-- A user may belong to many organizations through many rows here.
-- Soft-deleted rows keep history; uniqueness applies only while DeletedAt IS NULL.
CREATE TABLE [org].[StaffMembership]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Status] TINYINT NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_StaffMembership]
        PRIMARY KEY NONCLUSTERED ([Id]),

    CONSTRAINT [CK_StaffMembership_Status]
        CHECK ([Status] IN (1, 2, 3)),

    CONSTRAINT [FK_StaffMembership_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id]),

    CONSTRAINT [FK_StaffMembership_User]
        FOREIGN KEY ([UserId])
        REFERENCES [dbo].[User] ([Id])
);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_StaffMembership_OrganizationId]
    ON [org].[StaffMembership] ([OrganizationId], [Id]);
GO

CREATE UNIQUE INDEX [UX_StaffMembership_Organization_User]
    ON [org].[StaffMembership] ([OrganizationId], [UserId])
    WHERE [DeletedAt] IS NULL;
GO

CREATE INDEX [IX_StaffMembership_User]
    ON [org].[StaffMembership] ([UserId], [Status])
    INCLUDE ([OrganizationId])
    WHERE [DeletedAt] IS NULL;
GO
