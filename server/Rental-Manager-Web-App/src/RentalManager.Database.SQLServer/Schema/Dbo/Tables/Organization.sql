-- Global registry of tenants. Global level data: no OrganizationId column and
-- no row level security, because a row here identifies an organization rather
-- than belonging to one.
CREATE TABLE [dbo].[Organization]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [NormalizedName] NVARCHAR(256) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Organization_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,

    CONSTRAINT [PK_Organization]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [UQ_Organization_NormalizedName]
        UNIQUE ([NormalizedName])
);
