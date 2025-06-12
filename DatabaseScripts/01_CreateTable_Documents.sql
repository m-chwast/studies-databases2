-- Create the Documents table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' and xtype='U')
CREATE TABLE Documents (
    DocumentId INT PRIMARY KEY IDENTITY(1,1),
    DocumentName NVARCHAR(255) NOT NULL UNIQUE,
    Content NVARCHAR(MAX) NOT NULL
);
GO

-- Optional: Add an index on DocumentName for faster lookups if needed,
-- though for a small number of documents, it might not be critical.
-- IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Documents_DocumentName' AND object_id = OBJECT_ID('Documents'))
-- CREATE INDEX IX_Documents_DocumentName ON Documents(DocumentName);
-- GO
