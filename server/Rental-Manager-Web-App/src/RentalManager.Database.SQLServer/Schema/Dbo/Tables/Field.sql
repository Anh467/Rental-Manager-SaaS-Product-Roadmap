CREATE TABLE [dbo].[Field]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [FieldTypeId] INT NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Field_IsActive] DEFAULT (1),
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Field]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_Field_FieldType]
        FOREIGN KEY ([FieldTypeId])
        REFERENCES [dbo].[FieldType] ([Id]),

    CONSTRAINT [UQ_Field_Key]
        UNIQUE ([Key])
);
