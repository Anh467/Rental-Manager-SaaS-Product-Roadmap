-- Organization-level custom field definition. Shared table, isolated per
-- organization by row level security plus an explicit OrganizationId predicate
-- in every generated statement.
CREATE TABLE [org].[Field]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [TargetEntityType] NVARCHAR(64) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [NormalizedKey] NVARCHAR(256) NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [FieldTypeId] INT NOT NULL,
    [IsRequired] BIT NOT NULL CONSTRAINT [DF_Field_IsRequired] DEFAULT (0),
    [IsPrimaryDisplayField] BIT NOT NULL CONSTRAINT [DF_Field_IsPrimaryDisplayField] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Field_IsActive] DEFAULT (1),
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_Field_DisplayOrder] DEFAULT (0),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- Non-clustered so the clustered key can lead with OrganizationId and every
    -- organization-scoped query seeks instead of scans.
    CONSTRAINT [PK_Field_Org]
        PRIMARY KEY NONCLUSTERED ([Id]),

    -- Not filtered on DeletedAt: the key of a soft-deleted field stays reserved.
    CONSTRAINT [UQ_Field_OrganizationTargetKey]
        UNIQUE ([OrganizationId], [TargetEntityType], [NormalizedKey]),

    CONSTRAINT [FK_Field_Org_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id]),

    CONSTRAINT [FK_Field_Org_FieldType]
        FOREIGN KEY ([FieldTypeId])
        REFERENCES [dbo].[FieldType] ([Id])
);
GO

CREATE CLUSTERED INDEX [IX_Field_Org_OrganizationId]
    ON [org].[Field] ([OrganizationId], [Id]);
GO

-- Enforces the primary display field invariant in the database, so two
-- concurrent requests can never both win.
CREATE UNIQUE INDEX [UX_Field_ActivePrimary]
    ON [org].[Field] ([OrganizationId], [TargetEntityType])
    WHERE [IsPrimaryDisplayField] = 1
      AND [IsActive] = 1
      AND [DeletedAt] IS NULL;
GO

CREATE INDEX [IX_Field_Org_OrganizationTarget]
    ON [org].[Field] ([OrganizationId], [TargetEntityType], [IsActive])
    INCLUDE ([DisplayOrder], [Name]);
GO
