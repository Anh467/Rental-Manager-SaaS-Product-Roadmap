-- Global identity. Membership of one or more organizations is held by
-- [org].[OrganizationUser], not by columns on this table.
CREATE TABLE [dbo].[User]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [NormalizedEmail] NVARCHAR(256) NOT NULL,
    [DisplayName] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(512) NOT NULL,
    [PasswordSalt] NVARCHAR(512) NOT NULL,
    [GlobalRoleId] UNIQUEIDENTIFIER NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_User_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,

    CONSTRAINT [PK_User]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [UQ_User_NormalizedEmail]
        UNIQUE ([NormalizedEmail]),

    CONSTRAINT [FK_User_GlobalRole]
        FOREIGN KEY ([GlobalRoleId])
        REFERENCES [dbo].[Role] ([Id])
);
