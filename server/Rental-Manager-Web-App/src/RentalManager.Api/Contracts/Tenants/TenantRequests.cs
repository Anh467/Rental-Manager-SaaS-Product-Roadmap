namespace RentalManager.Api.Contracts.Tenants;

public sealed record CreateTenantRequest(
    string Code,
    string Name);

public sealed record UpdateTenantRequest(
    string Name);
