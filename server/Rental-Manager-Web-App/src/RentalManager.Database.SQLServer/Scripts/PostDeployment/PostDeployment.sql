:r .\SeedDatas\FieldType.sql
:r .\SeedDatas\Permission.sql
:r .\SeedDatas\GlobalAdminRole.sql

-- WITH EXECUTE AS N'OrganizationMembershipResolver' on the login membership
-- procedure requires the impersonated WITHOUT LOGIN user to be able to access
-- this database. When the database owner is not a sysadmin, SQL Server only
-- allows that after TRUSTWORTHY is enabled for this database.
DECLARE @EnableTrustworthy nvarchar(300) =
    N'ALTER DATABASE ' + QUOTENAME(DB_NAME()) + N' SET TRUSTWORTHY ON;';
EXEC (@EnableTrustworthy);
