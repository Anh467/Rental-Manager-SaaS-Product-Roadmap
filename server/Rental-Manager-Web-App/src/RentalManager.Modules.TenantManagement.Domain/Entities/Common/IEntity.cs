using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common
{
    public interface IEntity<T> where T : notnull
    {
        T Id { get; set; }
    }
}
