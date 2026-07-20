# Backend architecture

The backend is a modular monolith using Clean Architecture boundaries and logical CQRS.

## Project responsibilities

```text
RentalManager.Api
  -> HTTP, middleware, authentication, authorization and composition root

RentalManager.Modules.TenantManagement.Application
  -> use cases, commands, queries and handlers

RentalManager.Modules.TenantManagement.Core
  -> stable module abstractions, contracts, constants, permissions and CQRS interfaces

RentalManager.Modules.TenantManagement.Domain
  -> aggregates, entities, value objects, business rules and domain events

RentalManager.Modules.TenantManagement.Infrastructure
  -> SQL access, repositories, read stores, transactions and external adapters

RentalManager.BuildingBlocks.Tenancy
  -> current-tenant context shared across modules

RentalManager.Database.SQLServer
  -> SQL Server schema source of truth and DACPAC artifact
```

## Allowed project references

```text
Core              -> Domain
Application       -> Core + Domain + BuildingBlocks.Tenancy
Infrastructure    -> Core + Domain + BuildingBlocks.Tenancy
Identity Infra    -> Identity Application + BuildingBlocks.Tenancy
Api               -> Application + Infrastructure + BuildingBlocks.Tenancy
Database.sqlproj  -> no C# project references
```

Application must never reference Infrastructure. Domain must never reference Core, Application, Infrastructure or Api.

## TenantManagement vertical slice

```text
Application/Tenants/<UseCase>/
  <UseCase>Command.cs or <UseCase>Query.cs
  <UseCase>Handler.cs
  <UseCase>Result.cs
  <UseCase>Validator.cs       # add when validation library is selected
```

Current examples:

- `CreateTenant`
- `UpdateTenant`
- `ActivateTenant`
- `SuspendTenant`
- `GetTenantById`
- `SearchTenants`

## Command flow

```text
HTTP request
  -> Command
  -> CommandHandler
  -> Domain aggregate behavior
  -> Repository
  -> UnitOfWork
  -> Write connection
```

Commands change state. Business rules belong in Domain. Use-case orchestration belongs in Application.

## Query flow

```text
HTTP request
  -> Query
  -> QueryHandler
  -> ReadStore
  -> Read connection
  -> DTO
```

Queries do not change state and do not need to load a complete aggregate.

## Database strategy

The current configuration has separate logical connection strings:

- `RentalManagerWrite`
- `RentalManagerRead`

Both point to the same SQL Server database for the MVP. Later, `RentalManagerRead` can point to a readable replica without changing Application handlers.

The DACPAC is built from `RentalManager.Database.SQLServer.sqlproj` and published to the primary database only when using a read replica.

## Adding a new feature

1. Add or update business behavior in Domain.
2. Add stable interfaces or shared contracts in Core only when more than one layer needs them.
3. Add one vertical slice in Application.
4. Implement persistence or external adapters in Infrastructure.
5. Register handlers and adapters in the Api composition root.
6. Add or update SQL schema in the Database project.
7. Add Domain, Application, Infrastructure and API tests at the appropriate level.

## Placement rules

- Business enum: Domain, next to its aggregate.
- Module permission or feature name: Core.
- Repository interface: Core.
- Command, query, handler and use-case response: Application.
- SQL, Dapper/ADO.NET, filesystem, message broker and provider implementation: Infrastructure.
- HTTP request/response and middleware: Api.
- Cross-module primitive: a BuildingBlocks project, never another module's Core project.
