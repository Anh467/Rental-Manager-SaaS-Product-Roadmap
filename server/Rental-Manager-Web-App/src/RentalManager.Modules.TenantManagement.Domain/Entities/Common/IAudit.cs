using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common
{
    public interface IAudit
    {
        DateTimeOffset UpdatedAt { get; set; }
        DateTimeOffset CreatedAt { get; set; }
        DateTimeOffset? DeletedAt { get; set; }
    }
}
