using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common
{
    public interface IDefinition
    {
        string Name { get; set; }
        string Key { get; set; }
        string? Description { get; set; }
    }
}
