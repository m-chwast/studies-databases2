IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_ClobCatalog')
CREATE FULLTEXT CATALOG ft_ClobCatalog AS DEFAULT;
GO

--    Use: SELECT name FROM sys.key_constraints WHERE type = 'PK' AND PARENT_OBJECT_ID = OBJECT_ID('Documents')

IF EXISTS (SELECT * FROM sys.fulltext_indexes fti JOIN sys.objects o ON fti.object_id = o.object_id WHERE o.name = 'Documents')
DROP FULLTEXT INDEX ON Documents;
GO

IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes fti JOIN sys.objects o ON fti.object_id = o.object_id WHERE o.name = 'Documents')
CREATE FULLTEXT INDEX ON Documents(
    Content LANGUAGE 1045, -- Polish language for content
    DocumentName LANGUAGE 1045 -- Polish language for document name (or 0 for neutral if preferred)
)
KEY INDEX PK__Document__DocumentId -- Placeholder: Replace with actual PK constraint name or unique index name on DocumentId.
ON ft_ClobCatalog
WITH CHANGE_TRACKING AUTO;
GO
