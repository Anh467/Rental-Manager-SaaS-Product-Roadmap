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
    -- OpenID Connect 'sub' stored exactly as issued (no trim/normalize).
    -- BIN2 alone is not enough for trailing-space identity: SQL Server ANSI
    -- padding can treat N'abc' and N'abc ' as equal. SubjectExactKey holds the
    -- exact UTF-16LE byte sequence of Subject so uniqueness and lookup are
    -- character-for-character.
    [Subject] NVARCHAR(256) COLLATE Latin1_General_BIN2 NOT NULL,
    [SubjectExactKey] AS (CONVERT(VARBINARY(512), [Subject])) PERSISTED NOT NULL,
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

-- Concurrent first logins for the same exact subject race on this constraint,
-- which is what makes provisioning idempotent rather than duplicating the user.
-- Uniqueness is on the exact byte key, not Subject text equality.
CREATE UNIQUE NONCLUSTERED INDEX [UX_UserIdentity_Provider_SubjectExactKey]
    ON [dbo].[UserIdentity] ([Provider], [SubjectExactKey]);
GO

CREATE NONCLUSTERED INDEX [IX_UserIdentity_UserId]
    ON [dbo].[UserIdentity] ([UserId]);
GO
