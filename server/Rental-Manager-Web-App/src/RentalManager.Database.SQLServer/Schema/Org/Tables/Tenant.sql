CREATE TABLE [org].[Tenant]
(
    [TenantId] UNIQUEIDENTIFIER NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Status] INT NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NULL,

    CONSTRAINT [PK_Tenant]
        PRIMARY KEY CLUSTERED ([TenantId]),

    CONSTRAINT [UQ_Tenant_Code]
        UNIQUE ([Code]),

    CONSTRAINT [CK_Tenant_Status]
        CHECK ([Status] IN (1, 2))
);
