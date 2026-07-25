namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common;

/// <summary>
/// Marks an entity whose table carries a SQL Server <c>ROWVERSION</c> column
/// used for optimistic concurrency.
/// </summary>
public interface IConcurrencyAware
{
    byte[] RowVersion { get; set; }
}
