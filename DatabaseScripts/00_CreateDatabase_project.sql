IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'project')
BEGIN
    CREATE DATABASE project;
END;
GO

PRINT 'Database project created or already exists.';
GO
