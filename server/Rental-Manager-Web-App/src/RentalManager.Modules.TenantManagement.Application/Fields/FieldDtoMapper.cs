using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields;

internal static class FieldDtoMapper
{
    public static FieldDto ToDto(Field field, IReadOnlyList<FieldOption> options)
    {
        return new FieldDto
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
            Options = options
                .Where(option => option.DeletedAt is null)
                .Select(option => new FieldOptionDto
                {
                    Id = option.Id,
                    Key = option.Key,
                    Name = option.Name,
                    Description = option.Description,
                    DisplayOrder = option.DisplayOrder,
                    IsActive = option.IsActive
                })
                .ToArray()
        };
    }

    public static string? NormalizeDescription(string? description)
    {
        string? trimmed = description?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
