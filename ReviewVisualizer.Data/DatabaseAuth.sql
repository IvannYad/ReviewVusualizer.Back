CREATE LOGIN Visitor WITH PASSWORD = 'Pass%word123', DEFAULT_DATABASE = ReviewVisualizerDB
CREATE LOGIN Analyst WITH PASSWORD = 'Pass%word123', DEFAULT_DATABASE = ReviewVisualizerDB
CREATE LOGIN GeneratorAdmin WITH PASSWORD = 'Pass%word123', DEFAULT_DATABASE = ReviewVisualizerDB

/*-------------------------------------------------------------------------------------------------------*/

CREATE USER Visitor FOR LOGIN Visitor
WITH DEFAULT_SCHEMA = dbo;

CREATE USER Analyst FOR LOGIN Analyst
WITH DEFAULT_SCHEMA = dbo;

CREATE USER GeneratorAdmin FOR LOGIN GeneratorAdmin
WITH DEFAULT_SCHEMA = dbo;
GO;

/*-------------------------------------------------------------------------------------------------------*/

CREATE SCHEMA Visualizer AUTHORIZATION dbo;
GO;
CREATE SCHEMA Generator AUTHORIZATION dbo;
GO;
CREATE SCHEMA Shared AUTHORIZATION dbo;
GO;

ALTER SCHEMA Visualizer TRANSFER dbo.Departments;
ALTER SCHEMA Visualizer TRANSFER dbo.Teachers;
ALTER SCHEMA Visualizer TRANSFER dbo.Analysts;

ALTER SCHEMA Generator TRANSFER dbo.Reviewers;

ALTER SCHEMA Shared TRANSFER dbo.ReviewerTeacher;
ALTER SCHEMA Shared TRANSFER dbo.Reviews;

/*-------------------------------------------------------------------------------------------------------*/
/* Configure access for Visitor role */
DENY CONTROL ON SCHEMA::dbo TO Visitor;
DENY CONTROL ON SCHEMA::Hangfire TO Visitor;
DENY CONTROL ON SCHEMA::Generator TO Visitor;
DENY CONTROL ON [Shared].[ReviewerTeacher] TO Visitor;

GRANT SELECT ON SCHEMA::Visualizer TO Visitor;
GRANT SELECT ON SCHEMA::Shared TO Visitor;

/*-------------------------------------------------------------------------------------------------------*/
/* Configure access for Analyst role */
DENY CONTROL ON SCHEMA::dbo TO Analyst;
DENY CONTROL ON SCHEMA::Hangfire TO Analyst;
DENY CONTROL ON SCHEMA::Generator TO Analyst;

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::Visualizer TO Analyst;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::Shared TO Analyst;

/*-------------------------------------------------------------------------------------------------------*/
/* Configure access for GeneratorAdmin role */
DENY CONTROL ON SCHEMA::dbo TO GeneratorAdmin;
DENY CONTROL ON SCHEMA::Hangfire TO GeneratorAdmin;
DENY CONTROL ON SCHEMA::Visualizer TO GeneratorAdmin;

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::Generator TO GeneratorAdmin;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::Shared TO GeneratorAdmin;


  ALTER TABLE [ReviewVisualizerDB].[Shared].[Reviews]
ALTER COLUMN [ReviewTime] DATETIME MASKED WITH (FUNCTION = 'default()');
