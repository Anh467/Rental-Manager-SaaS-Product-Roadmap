SET NOCOUNT ON;

DECLARE @FieldTypes TABLE
(
    [Id] INT NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Key] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(1028) NULL
);

INSERT INTO @FieldTypes ([Id], [Name], [Key], [Description])
VALUES
    (1, N'Text',      N'TEXT',      N'Free-form text value.'),
    (2, N'Number',    N'NUMBER',    N'Integer or decimal numeric value.'),
    (3, N'Date',      N'DATE',      N'Date or date-time value.'),
    (4, N'Boolean',   N'BOOLEAN',   N'True or false value.'),
    (5, N'Selection', N'SELECTION', N'Value selected from predefined options.');

UPDATE target
SET
    target.[Name] = source.[Name],
    target.[Key] = source.[Key],
    target.[Description] = source.[Description]
FROM [dbo].[FieldType] AS target
INNER JOIN @FieldTypes AS source
    ON source.[Id] = target.[Id];

INSERT INTO [dbo].[FieldType]
(
    [Id],
    [Name],
    [Key],
    [Description]
)
SELECT
    source.[Id],
    source.[Name],
    source.[Key],
    source.[Description]
FROM @FieldTypes AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[FieldType] AS target
    WHERE target.[Id] = source.[Id]
);
