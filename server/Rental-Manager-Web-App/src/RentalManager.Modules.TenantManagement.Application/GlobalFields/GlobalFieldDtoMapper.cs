using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields;

internal static class GlobalFieldDtoMapper
{
    public static FieldDto ToDto(Field field, IReadOnlyList<FieldOption> items) => new()
    {
        Id = field.Id,
        Key = field.Key,
        Name = field.Name,
        Description = field.Description,
        FieldTypeId = field.FieldTypeId,
        IsActive = field.IsActive,
        CreatedAt = field.CreatedAt,
        UpdatedAt = field.UpdatedAt,
        RowVersion = Convert.ToBase64String(field.RowVersion),
        Options = items.Select(x => new FieldOptionDto
        {
            Id = x.Id,
            Key = x.Key,
            Name = x.Name,
            Description = x.Description,
            DisplayOrder = x.DisplayOrder,
            IsActive = x.IsActive
        }).ToArray()
    };

    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
