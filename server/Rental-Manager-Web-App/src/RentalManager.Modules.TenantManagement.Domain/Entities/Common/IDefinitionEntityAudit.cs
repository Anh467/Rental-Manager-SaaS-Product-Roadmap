using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common
{
    public interface IDefinitionEntityAudit<T> : IEntity<T>, IDefinition,  IAudit where T : notnull
    {
    }
}
