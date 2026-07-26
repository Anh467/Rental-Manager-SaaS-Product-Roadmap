using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

/// <summary>
/// Synchronizes global field options inside an ambient transaction owned by the
/// calling command handler.
/// </summary>
public sealed class GlobalFieldOptionSynchronizer(IGlobalFieldOptionRepository options)
{
    public async Task<IReadOnlyList<FieldOption>> ReplaceAsync(
        Field field,
        IReadOnlyList<FieldOptionInput>? inputs,
        CancellationToken cancellationToken)
    {
        var existing = await options.GetByFieldIdAsync(field.Id, cancellationToken);
        if (!FieldInvariants.RequiresOptions(field.FieldTypeId))
        {
            foreach (var item in existing)
            {
                await options.RetireAsync(item.Id, cancellationToken);
            }

            return [];
        }

        var result = new List<FieldOption>();
        var retained = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in inputs ?? [])
        {
            retained.Add(input.Key!);
            var item = existing.FirstOrDefault(x => x.Key == input.Key);
            if (item is null)
            {
                item = new FieldOption
                {
                    Id = Guid.CreateVersion7(),
                    FieldId = field.Id,
                    Key = input.Key!,
                    Name = input.Name!.Trim(),
                    Description = GlobalFieldDtoMapper.Clean(input.Description),
                    DisplayOrder = input.DisplayOrder,
                    IsActive = input.IsActive
                };
                await options.InsertAsync(item, cancellationToken);
            }
            else
            {
                item.Name = input.Name!.Trim();
                item.Description = GlobalFieldDtoMapper.Clean(input.Description);
                item.DisplayOrder = input.DisplayOrder;
                item.IsActive = input.IsActive;
                await options.UpdateAsync(item, item.RowVersion, cancellationToken);
            }

            result.Add(item);
        }

        foreach (var item in existing.Where(x => !retained.Contains(x.Key)))
        {
            await options.RetireAsync(item.Id, cancellationToken);
        }

        return result.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToArray();
    }
}
