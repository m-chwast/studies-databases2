-- projectTest database setup

USE master; -- Switch to master database context to allow dropping projectTest
GO

IF DB_ID('projectTest') IS NOT NULL
BEGIN
    PRINT 'Database projectTest exists. Attempting to drop...';
    -- Set to single user mode to close active connections
    ALTER DATABASE projectTest SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE projectTest;
    PRINT 'Old database projectTest dropped.';
END
ELSE
BEGIN
    PRINT 'Database projectTest does not exist. No need to drop.';
END
GO

CREATE DATABASE projectTest;

PRINT 'Database projectTest created.';
GO

USE projectTest;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' and xtype='U')
CREATE TABLE Documents (
    DocumentId INT IDENTITY(1,1),
    DocumentName NVARCHAR(255) NOT NULL UNIQUE,
    Content NVARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_Documents PRIMARY KEY (DocumentId)
);
GO

PRINT 'Table Documents created or already exists in project database with explicit PK_Documents.';
GO

IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_ClobCatalog_projectTest')
CREATE FULLTEXT CATALOG ft_ClobCatalog_projectTest AS DEFAULT;
GO

IF EXISTS (SELECT * FROM sys.fulltext_indexes fti JOIN sys.objects o ON fti.object_id = o.object_id WHERE o.name = 'Documents' AND o.schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT 'Full-Text Index on Documents table already exists. Dropping it.';
    DROP FULLTEXT INDEX ON dbo.Documents;
END
GO

PRINT 'Attempting to create Full-Text Index on Documents table...';
CREATE FULLTEXT INDEX ON dbo.Documents (
    Content LANGUAGE 1033,
    DocumentName LANGUAGE 1033
)
KEY INDEX PK_Documents
ON ft_ClobCatalog_projectTest
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Full-Text Index on Documents table creation attempted.';
GO

PRINT 'projectTest database setup completed.';
GO
