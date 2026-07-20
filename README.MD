# Rental Manager SaaS

## Backend architecture

```text
RentalManager.Api
├── TenantManagement.Application
├── TenantManagement.Infrastructure
├── Identity.Application
├── Identity.Infrastructure
└── BuildingBlocks.Tenancy

TenantManagement.Application
├── TenantManagement.Core
├── TenantManagement.Domain
└── BuildingBlocks.Tenancy

TenantManagement.Infrastructure
├── TenantManagement.Core
├── TenantManagement.Domain
└── BuildingBlocks.Tenancy

TenantManagement.Core
└── TenantManagement.Domain

TenantManagement.Domain
└── no project references
```

## Layer responsibilities

| Project | Responsibility |
|---|---|
| Domain | Entities, value objects, business enums and business rules |
| Core | Stable module abstractions, CQRS contracts, persistence ports and permissions |
| Application | Use cases, commands, queries and handlers |
| Infrastructure | SQL Server, repositories and external service implementations |
| BuildingBlocks.Tenancy | Current tenant context shared across modules |
| API | HTTP endpoints and composition root |
| Database.SQLServer | SQL schema source of truth, built and deployed as a DACPAC |

## TenantManagement base structure

```text
TenantManagement.Domain/
└── Tenants/
    ├── Tenant.cs
    └── TenantStatus.cs

TenantManagement.Core/
├── Abstractions/
│   ├── Messaging/
│   ├── Persistence/
│   └── Services/
├── Contracts/
└── Permissions/

TenantManagement.Application/
└── Tenants/
    ├── CreateTenant/
    └── GetTenantById/

TenantManagement.Infrastructure/
├── Persistence/
└── Services/
```

The SQL Database Project is independent from the C# projects. It produces the DACPAC used to update the SQL Server schema.
