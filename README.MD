                         RentalManager.Api
                                  │
            ┌─────────────────────┼─────────────────────┐
            │                     │                     │
            ▼                     ▼                     ▼
 Identity.Infrastructure   TenantManagement.      BuildingBlocks.
                           Infrastructure          Tenancy
            │                     │
            ▼                     ▼
 Identity.Application      TenantManagement.Application
            │                     │
            │              ┌──────┴──────┐
            │              ▼             ▼
            └──────────► TenantManagement.Core
                           ▲
                           │
                  TenantManagement.Domain