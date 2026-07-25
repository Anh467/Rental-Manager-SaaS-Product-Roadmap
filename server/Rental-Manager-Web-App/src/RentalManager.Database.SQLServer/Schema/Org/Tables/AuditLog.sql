-- Append-only trail of configuration changes. ChangeSummary carries only the
-- names and values of configuration properties, never request payloads and
-- never tenant business data.
CREATE TABLE [org].[AuditLog]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
    [EntityType] NVARCHAR(64) NOT NULL,
    [EntityId] UNIQUEIDENTIFIER NOT NULL,
    [Action] NVARCHAR(32) NOT NULL,
    [PerformedByUserId] UNIQUEIDENTIFIER NULL,
    [PerformedAt] DATETIMEOFFSET NOT NULL,
    [ChangeSummary] NVARCHAR(2000) NULL,
    [CorrelationId] NVARCHAR(128) NULL,

    CONSTRAINT [PK_AuditLog]
        PRIMARY KEY NONCLUSTERED ([Id]),

    CONSTRAINT [FK_AuditLog_Organization]
        FOREIGN KEY ([OrganizationId])
        REFERENCES [dbo].[Organization] ([Id])
);
GO

CREATE CLUSTERED INDEX [IX_AuditLog_OrganizationEntity]
    ON [org].[AuditLog] ([OrganizationId], [EntityType], [EntityId], [PerformedAt]);
GO
