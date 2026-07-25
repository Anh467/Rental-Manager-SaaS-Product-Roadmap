-- Relational options for a MultiSelect field. Never stored as JSON, and the
-- foreign key makes an orphan option impossible.
CREATE TABLE [org].[FieldOption]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [FieldId] UNIQUEIDENTIFIER NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [NormalizedKey] NVARCHAR(256) NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_FieldOption_DisplayOrder] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_FieldOption_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_FieldOption]
        PRIMARY KEY NONCLUSTERED ([Id]),

    CONSTRAINT [UQ_FieldOption_FieldKey]
        UNIQUE ([OrganizationId], [FieldId], [NormalizedKey]),

    CONSTRAINT [FK_FieldOption_Field]
        FOREIGN KEY ([FieldId])
        REFERENCES [org].[Field] ([Id]),

    CONSTRAINT [FK_FieldOption_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id])
);
GO

CREATE CLUSTERED INDEX [IX_FieldOption_OrganizationField]
    ON [org].[FieldOption] ([OrganizationId], [FieldId], [DisplayOrder]);
GO
