namespace RentalManager.BuildingBlocks.Tenancy;

public sealed record TenantContext(
    Guid TenantId,
    string? TenantCode = null);
