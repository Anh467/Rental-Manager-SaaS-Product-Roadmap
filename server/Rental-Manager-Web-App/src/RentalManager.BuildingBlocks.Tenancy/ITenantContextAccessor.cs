namespace RentalManager.BuildingBlocks.Tenancy;

public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}
