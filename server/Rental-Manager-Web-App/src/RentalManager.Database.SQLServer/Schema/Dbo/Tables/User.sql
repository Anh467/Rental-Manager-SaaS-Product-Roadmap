-- Global identity. Each user can belong to one organization and have one
-- organization role.
CREATE TABLE [dbo].[User]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NULL,
    [RoleId] UNIQUEIDENTIFIER NULL,
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
        UNIQUE ([NormalizedEmail]),

    CONSTRAINT [CK_User_OrganizationRole]
        CHECK
        (
            ([OrganizationId] IS NULL AND [RoleId] IS NULL)
            OR
            ([OrganizationId] IS NOT NULL AND [RoleId] IS NOT NULL)
        ),

    CONSTRAINT [FK_User_OrganizationRole]
        FOREIGN KEY ([OrganizationId], [RoleId])
        REFERENCES [org].[Role] ([OrganizationId], [Id])
);
