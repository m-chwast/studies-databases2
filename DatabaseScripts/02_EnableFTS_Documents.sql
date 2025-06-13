USE testCLR
GO


IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' and xtype='U')
CREATE TABLE Documents (
    DocumentId INT PRIMARY KEY IDENTITY(1,1),
    DocumentName NVARCHAR(255) NOT NULL UNIQUE,
    Content NVARCHAR(MAX) NOT NULL
);
GO


IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_ClobCatalog')
CREATE FULLTEXT CATALOG ft_ClobCatalog AS DEFAULT;
GO

SELECT name FROM sys.key_constraints WHERE type = 'PK' AND PARENT_OBJECT_ID = OBJECT_ID('Documents')
GO


IF EXISTS (SELECT * FROM sys.fulltext_indexes fti JOIN sys.objects o ON fti.object_id = o.object_id WHERE o.name = 'Documents')
DROP FULLTEXT INDEX ON Documents;
GO

IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes fti JOIN sys.objects o ON fti.object_id = o.object_id WHERE o.name = 'Documents')
CREATE FULLTEXT INDEX ON Documents(
    Content LANGUAGE 1045, -- polski
    DocumentName LANGUAGE 1045 -- polski
)
KEY INDEX PK__Document__1ABEEF0F4B8E5BAE
ON ft_ClobCatalog
WITH CHANGE_TRACKING AUTO;
GO


SELECT * FROM Documents;
GO
