-- Representative pre-SCRUM-79/80/81 schema for upgrade tests.
-- Intentionally omits UserIdentity, PlatformRole*, StaffMembership, StaffRole.
-- PasswordHash/PasswordSalt remain NOT NULL (password-era identity).
-- Membership is org.OrganizationUser under RLS.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'org')
    EXEC(N'CREATE SCHEMA [org]');
GO

CREATE TABLE [dbo].[Organization]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Organization_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    CONSTRAINT [PK_Organization] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Organization_Key] UNIQUE ([Key])
);
GO

CREATE TABLE [dbo].[Role]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL,
    [Scope] INT NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Role_Key] UNIQUE ([Key])
);
GO

CREATE TABLE [dbo].[User]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [UserName] NVARCHAR(256) NOT NULL CONSTRAINT [DF_User_UserName] DEFAULT (N''),
    [NormalizedUserName] NVARCHAR(256) NOT NULL CONSTRAINT [DF_User_NormalizedUserName] DEFAULT (N''),
    [Email] NVARCHAR(256) NOT NULL,
    [NormalizedEmail] NVARCHAR(256) NOT NULL,
    [EmailConfirmed] BIT NOT NULL CONSTRAINT [DF_User_EmailConfirmed] DEFAULT (1),
    [DisplayName] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(512) NOT NULL,
    [PasswordSalt] NVARCHAR(512) NOT NULL,
    [SecurityStamp] NVARCHAR(36) NOT NULL CONSTRAINT [DF_User_SecurityStamp] DEFAULT (CONVERT(NVARCHAR(36), NEWID())),
    [ConcurrencyStamp] NVARCHAR(36) NOT NULL CONSTRAINT [DF_User_ConcurrencyStamp] DEFAULT (CONVERT(NVARCHAR(36), NEWID())),
    [LockoutEnd] DATETIMEOFFSET NULL,
    [LockoutEnabled] BIT NOT NULL CONSTRAINT [DF_User_LockoutEnabled] DEFAULT (1),
    [AccessFailedCount] INT NOT NULL CONSTRAINT [DF_User_AccessFailedCount] DEFAULT (0),
    [GlobalRoleId] UNIQUEIDENTIFIER NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_User_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_User_GlobalRole] FOREIGN KEY ([GlobalRoleId]) REFERENCES [dbo].[Role] ([Id])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_User_NormalizedEmail]
    ON [dbo].[User] ([NormalizedEmail]) WHERE [DeletedAt] IS NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_User_NormalizedUserName]
    ON [dbo].[User] ([NormalizedUserName]) WHERE [DeletedAt] IS NULL;
GO

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
    CONSTRAINT [PK_Role] PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UQ_Role_OrganizationKey] UNIQUE ([OrganizationId], [Key]),
    CONSTRAINT [FK_Role_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organization] ([Id])
);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_Role_OrganizationId]
    ON [org].[Role] ([OrganizationId], [Id]);
GO

CREATE TABLE [org].[OrganizationUser]
(
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_OrganizationUser_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    CONSTRAINT [PK_OrganizationUser] PRIMARY KEY CLUSTERED ([OrganizationId], [UserId]),
    CONSTRAINT [FK_OrganizationUser_Role]
        FOREIGN KEY ([OrganizationId], [RoleId])
        REFERENCES [org].[Role] ([OrganizationId], [Id]),
    CONSTRAINT [FK_OrganizationUser_User]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id])
);
GO

CREATE FUNCTION [org].[fn_OrganizationAccessPredicate]
(
    @OrganizationId UNIQUEIDENTIFIER
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS [fn_AccessResult]
    WHERE @OrganizationId = CAST(SESSION_CONTEXT(N'OrganizationId') AS UNIQUEIDENTIFIER);
GO

CREATE SECURITY POLICY [org].[OrganizationIsolationPolicy]
    ADD FILTER PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Role],
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Role] AFTER INSERT,
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Role] AFTER UPDATE,
    ADD FILTER PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[OrganizationUser],
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[OrganizationUser] AFTER INSERT,
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[OrganizationUser] AFTER UPDATE
WITH (STATE = ON, SCHEMABINDING = ON);
GO
