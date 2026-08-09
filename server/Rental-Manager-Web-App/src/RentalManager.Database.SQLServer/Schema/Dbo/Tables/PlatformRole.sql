-- Platform-scoped role. Distinct from dbo.Role (configuration template) and
-- org.Role (organization staff). Assignments live in dbo.PlatformUserRole.
CREATE TABLE [dbo].[PlatformRole]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Key] NVARCHAR(128) NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [IsActive] BIT NOT NULL
        CONSTRAINT [DF_PlatformRole_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,

    CONSTRAINT [PK_PlatformRole]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [UQ_PlatformRole_Key]
        UNIQUE ([Key])
);
GO
