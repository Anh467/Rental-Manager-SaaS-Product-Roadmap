-- Roles are defined per organization so each tenant can name and compose them
-- independently, while the permissions they grant come from the global catalogue.
CREATE TABLE [org].[Role]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [IsSystemRole] BIT NOT NULL CONSTRAINT [DF_Role_IsSystemRole] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Role_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Role]
        PRIMARY KEY NONCLUSTERED ([Id]),

    CONSTRAINT [UQ_Role_OrganizationKey]
        UNIQUE ([OrganizationId], [Key]),

    CONSTRAINT [FK_Role_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id])
);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_Role_OrganizationId]
    ON [org].[Role] ([OrganizationId], [Id]);
GO
