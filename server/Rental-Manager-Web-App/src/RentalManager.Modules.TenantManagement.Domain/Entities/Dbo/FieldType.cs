using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Dbo
{
    [Table(nameof(FieldType), Schema = DatabaseConstant.Schema.DBO)]
    public class FieldType : IEntity<int>, IDefinition
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Key { get; set; }
        public string? Description { get; set; }
    }
}
