-- Global identity. Membership of one or more organizations is held by
-- [org].[OrganizationUser], not by columns on this table.
-- Identity Core columns live on this table (no AspNetUsers).
CREATE TABLE [dbo].[User]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [UserName] NVARCHAR(256) NOT NULL
        CONSTRAINT [DF_User_UserName] DEFAULT (N''),
    [NormalizedUserName] NVARCHAR(256) NOT NULL
        CONSTRAINT [DF_User_NormalizedUserName] DEFAULT (N''),
    [Email] NVARCHAR(256) NOT NULL,
    [NormalizedEmail] NVARCHAR(256) NOT NULL,
    [EmailConfirmed] BIT NOT NULL
        CONSTRAINT [DF_User_EmailConfirmed] DEFAULT (1),
    [DisplayName] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(512) NOT NULL,
    -- Retained for legacy PBKDF2 verify until Identity rehash clears it.
    [PasswordSalt] NVARCHAR(512) NULL,
    [SecurityStamp] NVARCHAR(36) NOT NULL
        CONSTRAINT [DF_User_SecurityStamp] DEFAULT (CONVERT(NVARCHAR(36), NEWID())),
    [ConcurrencyStamp] NVARCHAR(36) NOT NULL
        CONSTRAINT [DF_User_ConcurrencyStamp] DEFAULT (CONVERT(NVARCHAR(36), NEWID())),
    [LockoutEnd] DATETIMEOFFSET NULL,
    [LockoutEnabled] BIT NOT NULL
        CONSTRAINT [DF_User_LockoutEnabled] DEFAULT (1),
    [AccessFailedCount] INT NOT NULL
        CONSTRAINT [DF_User_AccessFailedCount] DEFAULT (0),
    [GlobalRoleId] UNIQUEIDENTIFIER NULL,
    [IsActive] BIT NOT NULL
        CONSTRAINT [DF_User_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_User]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_User_GlobalRole]
        FOREIGN KEY ([GlobalRoleId])
        REFERENCES [dbo].[Role] ([Id])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_User_NormalizedEmail]
    ON [dbo].[User] ([NormalizedEmail])
    WHERE [DeletedAt] IS NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_User_NormalizedUserName]
    ON [dbo].[User] ([NormalizedUserName])
    WHERE [DeletedAt] IS NULL;
GO
