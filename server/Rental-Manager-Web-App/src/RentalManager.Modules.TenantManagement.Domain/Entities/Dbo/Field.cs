using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Dbo
{
    [Table(nameof(Field), Schema = DatabaseConstant.Schema.DBO)]
    public class Field : IDefinitionEntityAudit<Guid>
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Key { get; set; }
        public string? Description { get; set; }
        public int FieldTypeId {  get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
