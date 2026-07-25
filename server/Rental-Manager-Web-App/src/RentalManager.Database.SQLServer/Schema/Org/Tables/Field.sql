-- Organization-level custom field definition. Shared table, isolated per
-- organization by row level security plus an explicit OrganizationId predicate
-- in every generated statement.
CREATE TABLE [org].[Field]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [FieldTypeId] INT NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Field_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- Non-clustered so the clustered key can lead with OrganizationId and every
    -- organization-scoped query seeks instead of scans.
    CONSTRAINT [PK_Field_Org]
        PRIMARY KEY NONCLUSTERED ([Id]),

    -- Not filtered on DeletedAt: the key of a soft-deleted field stays reserved.
    CONSTRAINT [UQ_Field_OrganizationKey]
        UNIQUE ([OrganizationId], [Key]),

    CONSTRAINT [FK_Field_Org_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id]),

    CONSTRAINT [FK_Field_Org_FieldType]
        FOREIGN KEY ([FieldTypeId])
        REFERENCES [dbo].[FieldType] ([Id])
);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_Field_Org_OrganizationId]
    ON [org].[Field] ([OrganizationId], [Id]);
GO
