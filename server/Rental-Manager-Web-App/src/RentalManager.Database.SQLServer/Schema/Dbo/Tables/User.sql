-- Global identity. A user is authenticated once and can then be authorized for
-- one or more organizations through [org].[OrganizationUser].
CREATE TABLE [dbo].[User]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [NormalizedEmail] NVARCHAR(256) NOT NULL,
    [DisplayName] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(512) NOT NULL,
    [PasswordSalt] NVARCHAR(512) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_User_IsActive] DEFAULT (1),
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [DeletedAt] DATETIMEOFFSET NULL,

    CONSTRAINT [PK_User]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [UQ_User_NormalizedEmail]
        UNIQUE ([NormalizedEmail])
);
