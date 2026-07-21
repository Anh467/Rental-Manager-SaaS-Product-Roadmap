namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common;

public interface IDefinitionEntityAudit<TPrimaryKey> :
    IEntityAudit<TPrimaryKey>,
    IDefinition
    where TPrimaryKey : notnull
{
}
