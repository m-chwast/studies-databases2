IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'projectTest')
BEGIN
    CREATE DATABASE projectTest;
END;
GO

PRINT 'Database projectTest created or already exists.';
GO
