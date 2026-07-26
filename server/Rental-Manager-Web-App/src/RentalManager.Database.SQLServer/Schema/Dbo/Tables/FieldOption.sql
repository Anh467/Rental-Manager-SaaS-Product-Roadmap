CREATE TABLE [dbo].[FieldOption]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [FieldId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_FieldOption_DisplayOrder] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_FieldOption_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_FieldOption] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FieldOption_Field] FOREIGN KEY ([FieldId])
        REFERENCES [dbo].[Field] ([Id]),
    CONSTRAINT [UQ_FieldOption_FieldKey] UNIQUE ([FieldId], [Key])
);
