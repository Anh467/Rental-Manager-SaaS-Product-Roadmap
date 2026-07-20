# SQL Server Database Project

This project is the schema source of truth and builds the RentalManager DACPAC.

## Structure

```text
Schemas/
  org.sql

Schema/
  Dbo/
    Tables/
    Views/
    StoredProcedures/
    Functions/
    SecurityPolicies/
  Org/
    Tables/
    Views/
    StoredProcedures/
    Functions/
    SecurityPolicies/

Scripts/
  PreDeployment/
  PostDeployment/
    SeedDatas/
    DataFixes/
    Releases/

PublishProfiles/
```

## Rules

- Define each schema object once using its desired final state.
- Change the existing `CREATE TABLE` file when the table structure changes.
- Use refactoring support or an explicit migration strategy when renaming tables or columns.
- Keep `BlockOnPossibleDataLoss` enabled for staging and production.
- Use post-deployment scripts only for idempotent seed data or controlled data migration.
- Do not store database passwords in publish profiles.
- Generate and review the deployment script before publishing to production.

## Build and deployment

```text
SQL source
  -> SQL project build
  -> RentalManager.Database.SQLServer.dacpac
  -> SqlPackage Script / DeployReport
  -> SqlPackage Publish to primary database
```

When the application later uses a read replica, publish the DACPAC to the primary database only. SQL Server replication is responsible for propagating schema and data to the readable secondary.
