-- Maps a verified external identity onto [dbo].[User]. The identity key is the
-- provider-scoped subject, never the email address: email and profile claims
-- are display attributes that may change or be reused by the provider.
CREATE TABLE [dbo].[UserIdentity]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    -- Stable provider key configured per deployment, for example N'oidc'.
    -- Compared case-insensitively under the database collation.
    [Provider] NVARCHAR(64) NOT NULL,
    -- OpenID Connect 'sub'. Binary collation because a subject is
    -- case-sensitive and two subjects differing only in case are two
    -- different identities.
    [Subject] NVARCHAR(256) COLLATE Latin1_General_BIN2 NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [LastLoginAt] DATETIMEOFFSET NULL,

    CONSTRAINT [PK_UserIdentity]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_UserIdentity_User]
        FOREIGN KEY ([UserId])
        REFERENCES [dbo].[User] ([Id]),

    CONSTRAINT [CK_UserIdentity_Provider]
        CHECK (LEN(LTRIM(RTRIM([Provider]))) > 0),

    CONSTRAINT [CK_UserIdentity_Subject]
        CHECK (LEN(LTRIM(RTRIM([Subject]))) > 0)
);
GO

-- Concurrent first logins for the same subject race on this constraint, which
-- is what makes provisioning idempotent rather than duplicating the user.
CREATE UNIQUE NONCLUSTERED INDEX [UX_UserIdentity_Provider_Subject]
    ON [dbo].[UserIdentity] ([Provider], [Subject]);
GO

CREATE NONCLUSTERED INDEX [IX_UserIdentity_UserId]
    ON [dbo].[UserIdentity] ([UserId]);
GO
