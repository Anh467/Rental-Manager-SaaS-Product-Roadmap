# Test project blueprint

Create the following test projects as implementation grows:

```text
tests/
├── RentalManager.Modules.TenantManagement.Domain.UnitTests/
├── RentalManager.Modules.TenantManagement.Application.UnitTests/
├── RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests/
└── RentalManager.Api.FunctionalTests/
```

## Domain unit tests

Reference only `TenantManagement.Domain`.

Test aggregate creation, state transitions, value objects, business rules and domain events. No mocks, database or ASP.NET host.

## Application unit tests

Reference `TenantManagement.Application`, `Core` and `Domain`.

Mock Core abstractions such as `ITenantRepository`, `ITenantReadStore`, `IUnitOfWork` and `IClock`. Test one handler per use case.

## Infrastructure integration tests

Reference `TenantManagement.Infrastructure`, `Core` and `Domain`.

Deploy the DACPAC to an isolated SQL Server database or container. Test repositories, read stores, transactions, mappings and RLS session context.

## API functional tests

Reference `RentalManager.Api` and use `WebApplicationFactory<Program>`.

Test routes, model binding, middleware, authentication, authorization, tenant isolation and HTTP status codes.

## Naming

```text
MethodUnderTest_State_ExpectedResult
```

Examples:

```text
Create_ValidInput_RaisesTenantCreatedEvent
HandleAsync_DuplicateCode_ThrowsConflict
GetByIdAsync_MissingTenant_ReturnsNotFound
```
