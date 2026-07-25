-- Organization-scoped organization definition.
CREATE TABLE [org].[Organization]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Organization_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Organization]
        PRIMARY KEY NONCLUSTERED ([Id]),

    CONSTRAINT [UQ_Organization_OrganizationKey]
        UNIQUE ([OrganizationId], [Key]),

    CONSTRAINT [FK_Organization_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id])
);
GO

CREATE CLUSTERED INDEX [IX_Organization_OrganizationId]
    ON [org].[Organization] ([OrganizationId], [Id]);
GO
