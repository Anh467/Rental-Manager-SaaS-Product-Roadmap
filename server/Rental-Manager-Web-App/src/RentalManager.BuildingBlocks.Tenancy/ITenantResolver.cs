namespace RentalManager.BuildingBlocks.Tenancy;

public interface ITenantResolver<in TContext>
{
    ValueTask<TenantContext?> ResolveAsync(
        TContext context,
        CancellationToken cancellationToken = default);
}
