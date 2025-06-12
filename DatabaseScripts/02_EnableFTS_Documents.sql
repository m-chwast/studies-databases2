-- Ensure Full-Text Search is installed and enabled on the database.
-- This script assumes it is. You might need to enable it at the server/database level first.

-- 1. Create a Full-Text Catalog (if it doesn't exist)
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_ClobCatalog')
CREATE FULLTEXT CATALOG ft_ClobCatalog AS DEFAULT;
GO

-- 2. Create a Full-Text Index on the Documents table
--    IMPORTANT: You will likely need to find the correct name of the primary key constraint for DocumentId.
--    Use: SELECT name FROM sys.key_constraints WHERE type = 'PK' AND PARENT_OBJECT_ID = OBJECT_ID('Documents')
--    Then replace 'PK_Documents_DocumentId' with the actual constraint name found.
IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes fti JOIN sys.objects o ON fti.object_id = o.object_id WHERE o.name = 'Documents')
CREATE FULLTEXT INDEX ON Documents(
    Content LANGUAGE 1045 -- Polish language
)
KEY INDEX PK__Document__DocumentId -- Placeholder: Replace with actual PK constraint name (e.g., from the query above) or unique index name on DocumentId.
ON ft_ClobCatalog
WITH CHANGE_TRACKING AUTO;
GO

-- Note: After creating the Full-Text Index, it needs to be populated.
-- For CHANGE_TRACKING AUTO, SQL Server handles this.
-- For MANUAL, you would need to run:
-- ALTER FULLTEXT INDEX ON Documents START FULL POPULATION;
