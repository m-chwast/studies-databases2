-- project database setup

USE master; -- Switch to master database context to allow dropping project
GO

IF DB_ID('project') IS NOT NULL
BEGIN
    PRINT 'Database project exists. Attempting to drop...';
    ALTER DATABASE project SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE project;
    PRINT 'Old database project dropped.';
END
ELSE
BEGIN
    PRINT 'Database project does not exist. No need to drop.';
END
GO

CREATE DATABASE project;
PRINT 'Database project created.';
GO

USE project;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' and xtype='U')
CREATE TABLE Documents (
    DocumentId INT IDENTITY(1,1),
    DocumentName NVARCHAR(255) NOT NULL UNIQUE,
    Content NVARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_Documents PRIMARY KEY (DocumentId)
);
GO

PRINT 'Table Documents created or already exists in project database.';
GO

IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_ClobCatalog_project')
CREATE FULLTEXT CATALOG ft_ClobCatalog_project AS DEFAULT;
GO

IF EXISTS (SELECT * FROM sys.fulltext_indexes fti JOIN sys.objects o ON fti.object_id = o.object_id WHERE o.name = 'Documents' AND o.schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT 'Dropping existing Full-Text Index on Documents table.';
    DROP FULLTEXT INDEX ON dbo.Documents;
END
GO

PRINT 'Creating Full-Text Index on Documents table.';
CREATE FULLTEXT INDEX ON dbo.Documents (
    Content LANGUAGE 1033,
    DocumentName LANGUAGE 1033
)
KEY INDEX PK_Documents
ON ft_ClobCatalog_project
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Full-Text Index on Documents table created.';
GO

INSERT INTO Documents (DocumentName, Content) VALUES
('TechnicalManual.pdf', 'This technical manual contains detailed instructions for operating industrial machinery. It includes safety protocols, maintenance schedules, and troubleshooting guides for various equipment types.'),
('ProjectProposal.docx', 'Our project proposal outlines the development of a new customer relationship management system. The system will integrate with existing databases and provide real-time analytics for sales teams.'),
('MeetingNotes_2024.txt', 'Meeting notes from quarterly review: discussed budget allocations, team performance metrics, and upcoming product launches. Action items include hiring additional developers and upgrading server infrastructure.'),
('UserGuide_Software.pdf', 'User guide for the new software application. This document explains how to navigate the interface, configure settings, and utilize advanced features for maximum productivity.'),
('FinancialReport_Q3.xlsx', 'Third quarter financial report showing revenue growth of 15% compared to previous quarter. Key performance indicators include customer acquisition costs and lifetime value metrics.'),
('ResearchPaper_AI.docx', 'Research paper on artificial intelligence applications in healthcare. The study examines machine learning algorithms for diagnostic imaging and predictive analytics in patient care.'),
('PolicyDocument.pdf', 'Company policy document covering remote work guidelines, data security protocols, and employee benefits. Updated to reflect recent changes in labor regulations and industry standards.'),
('TrainingMaterial.pptx', 'Training material for new employee onboarding process. Covers company culture, organizational structure, and job-specific skills development programs for various departments.'),
('ContractAgreement.docx', 'Service contract agreement between our company and external vendors. Defines scope of work, delivery timelines, payment terms, and quality assurance requirements.'),
('TechnicalSpecification.txt', 'Technical specification document for the new mobile application. Details system requirements, API integrations, user interface design, and performance benchmarks for deployment.');
GO

PRINT 'Sample data inserted into Documents table.';
GO

PRINT 'project database script completed.';
GO
