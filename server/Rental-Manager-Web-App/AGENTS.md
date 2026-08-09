# Backend Instructions

These instructions extend the repository-root `AGENTS.md` for
`server/Rental-Manager-Web-App/**`.

## Current stack and entry points

- Target framework: .NET 10 with nullable reference types and implicit usings enabled.
- Solution used by CI: `Rental-Manager-Web-App.slnx`.
- API/composition root: `src/RentalManager.Api`.
- Shared tenancy building blocks: `src/RentalManager.BuildingBlocks.Tenancy`.
- Identity module: `src/RentalManager.Modules.Identity.Application` and
  `src/RentalManager.Modules.Identity.Infrastructure`.
- Tenant Management module: Domain, Application, Infrastructure, and legacy Core projects under
  `src/RentalManager.Modules.TenantManagement.*`.
- Persistence uses Dapper and SQL Server. The database source of truth is
  `src/RentalManager.Database.SQLServer/RentalManager.Database.SQLServer.sqlproj`.
- Do not introduce another ORM or persistence framework without an approved architectural decision.

## Dependency and ownership rules

- `RentalManager.Api` is the HTTP host and composition root. Keep controllers/endpoints thin: parse
  HTTP input, call an application use case, and translate the result to the canonical contract.
- Application projects own use cases, commands/queries, validation orchestration, and abstractions.
  They must not depend on ASP.NET request objects or concrete SQL implementations.
- Domain/Core code owns business invariants and domain types. It must not contain controllers,
  Dapper/SQL, HTTP status logic, configuration reads, provider SDKs, or localized messages.
- Infrastructure projects implement persistence, identity/session adapters, authorization adapters,
  and external services. Infrastructure depends inward; application/domain must not depend on it.
- Cross-module contracts belong in an existing neutral Building Block, or in one new neutral Building
  Block only when no suitable shared project exists. They must not live under a feature module merely
  because that module used them first.
- Before creating an abstraction, search all `src` and `tests`. Extend an existing abstraction when
  it has the same responsibility.

## API responses and errors

- Reuse the single canonical response types and message catalog; do not add controller-specific
  envelopes or message constants.
- Reuse one error-response writer across exception handling, model validation, authentication
  challenge, authorization forbidden, and rate limiting. Do not serialize the envelope separately at
  each call site.
- Keep exception-to-response mapping explicit. An unknown `DomainException` or any unmapped exception
  must fall back to `500 / ERR-050`, not a generic `409`.
- Baseline mapping:
  - malformed JSON, model binding, missing basic request fields, invalid CSRF: `400 / ERR-001`;
  - semantically valid JSON that violates field/business validation: `422 / ERR-001` with
    `fieldErrors`;
  - unauthenticated: `401 / ERR-003`;
  - missing permission: `403 / ERR-004`;
  - missing/invalid Organization Context: `403 / ERR-005`;
  - not found, including cross-organization resource probing: `404 / ERR-002`;
  - duplicate, stale version, or invalid state: `409` with the matching Active key;
  - rate limited: `429 / ERR-040`;
  - temporarily unavailable external/object-storage service: `503 / ERR-049` with a safe `service`
    parameter;
  - unexpected/unmapped error: `500 / ERR-050`.
- Never return stack traces, raw SQL, connection details, tokens, provider errors, object keys, or data
  from another Organization.

## Identity, authorization, and tenancy

- Authentication is OpenID Connect/provider-agnostic. Use the verified issuer/provider plus `sub` as
  the external identity. Never use email as the stable identity key and never store provider tokens or
  secrets in business tables.
- Do not add or revive a parallel runtime email/password login system. Development/bootstrap identity
  configuration uses safe provider/subject configuration, not committed passwords.
- First-login provisioning must be allow-listed, transaction-safe, idempotent, and safe under
  concurrent requests. Do not auto-link a different provider/subject solely because email matches.
- Enforce active User, Organization, StaffMembership, Role, and session/token state on protected
  requests where the accepted Story requires them. UI guards are never authorization.
- Use the existing permission authorization framework and canonical permission catalog. Do not create
  module-local permission systems or runtime permission CRUD unless Jira explicitly requires it.
- Resolve Organization Context from server-verified membership. Never trust OrganizationId, Role, or
  Permission claims supplied by the client without revalidation.
- The trusted Organization Context contains the verified UserId, OrganizationId, StaffMembershipId,
  and correlation ID. Effective permissions are the union of active roles only.
- Preserve non-disclosure: an authenticated user probing another Organization's resource receives the
  approved not-found response, not proof that the resource exists.
- RLS must remain safe with connection pooling. Set and clear SQL session context deliberately and
  retain integration coverage for cross-organization isolation.

## Database and migrations

- Every schema object belongs in `RentalManager.Database.SQLServer.sqlproj` under the appropriate
  `dbo`, `org`, or `Security` path. Ensure the SQL project and post-deployment entry point include new
  objects/scripts correctly.
- Seeds for permissions, roles, and reference data are idempotent and use canonical identifiers.
- Schema/data evolution must preserve production data. Do not rely on DACPAC automatically dropping,
  renaming, or recreating populated objects.
- For a model replacement, write an explicit cutover that validates preconditions, migrates data,
  checks counts/orphans/constraints, and fails safely. After cutover, remove old runtime reads/writes;
  do not keep legacy and replacement runtime models active in parallel.
- Keep OrganizationId in composite keys/constraints where needed to prevent cross-Organization
  references. Never broaden the application database user's grants or disable RLS to fix a test.
- Add database-clean publish coverage and an upgrade test from representative legacy schema/data for
  non-trivial migrations.

## Coding and test conventions

- Follow the established C# naming, async, cancellation-token, dependency-injection, and Dapper
  patterns in the nearest working feature. Do not add a generic repository when focused repositories
  already express the use case.
- Parameterize every SQL value. Keep transactions explicit for multi-step invariants and concurrent
  onboarding/state changes.
- Add unit tests for business rules and mapping. Add SQL integration tests for repositories,
  transactions, DACPAC deployment/upgrades, RLS, connection-pool isolation, and HTTP authorization
  behavior.
- Use fake/test authentication handlers; tests must not call a real identity provider or external
  service.
- Security/audit events use the shared publisher abstraction and safe structured fields with a
  correlation ID. Do not invent a durable audit store in a Story that only requires event publishing.

## Required backend checks

Run targeted tests while developing, then run the CI-equivalent commands from this directory:

```bash
dotnet restore Rental-Manager-Web-App.slnx
dotnet build Rental-Manager-Web-App.slnx --configuration Release --no-restore
dotnet test tests/RentalManager.Modules.TenantManagement.Application.UnitTests/RentalManager.Modules.TenantManagement.Application.UnitTests.csproj --configuration Release --no-build
dotnet test tests/RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests/RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests.csproj --configuration Release --no-build
```

If a new test project is added, register it in `Rental-Manager-Web-App.slnx` and CI, and run it in the
final validation. Do not report a database integration test as passed when SQL Server/DACPAC tooling
was unavailable; report the exact blocker.
