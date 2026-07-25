-- Global permission catalogue. Seeded by the database project; organizations
-- reference these rows from [org].[RolePermission] but never add to them.
CREATE TABLE [dbo].[Permission]
(
    [Id] INT NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [Module] NVARCHAR(64) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Permission_IsActive] DEFAULT (1),

    CONSTRAINT [PK_Permission]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [UQ_Permission_Key]
        UNIQUE ([Key])
);
