using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

/// <summary>
/// Synchronizes organization field options inside an ambient transaction owned
/// by the calling command handler.
/// </summary>
public sealed class OrgFieldOptionSynchronizer(IFieldOptionRepository fieldOptions)
{
    public async Task<IReadOnlyList<FieldOption>> ReplaceAsync(
        Field field,
        IReadOnlyList<FieldOptionInput>? inputs,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FieldOption> stored = await fieldOptions.GetByFieldIdAsync(
            field.Id,
            cancellationToken);

        if (!FieldInvariants.RequiresOptions(field.FieldTypeId))
        {
            foreach (FieldOption orphan in stored)
            {
                await fieldOptions.RetireAsync(orphan.Id, cancellationToken);
            }

            return [];
        }

        Dictionary<string, FieldOption> storedByKey = stored.ToDictionary(
            option => option.Key,
            StringComparer.Ordinal);

        var result = new List<FieldOption>();
        var keptKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (FieldOptionInput input in inputs ?? [])
        {
            string key = input.Key!;
            keptKeys.Add(key);

            if (storedByKey.TryGetValue(key, out FieldOption? existing))
            {
                existing.Name = input.Name!.Trim();
                existing.Description = FieldDtoMapper.NormalizeDescription(input.Description);
                existing.DisplayOrder = input.DisplayOrder;
                existing.IsActive = input.IsActive;

                await fieldOptions.UpdateAsync(
                    existing,
                    existing.RowVersion,
                    cancellationToken);

                result.Add(existing);
                continue;
            }

            var option = new FieldOption
            {
                Id = Guid.CreateVersion7(),
                FieldId = field.Id,
                Key = key,
                Name = input.Name!.Trim(),
                Description = FieldDtoMapper.NormalizeDescription(input.Description),
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive
            };

            await fieldOptions.InsertAsync(option, cancellationToken);
            result.Add(option);
        }

        foreach (FieldOption removed in stored.Where(
                     option => !keptKeys.Contains(option.Key)))
        {
            await fieldOptions.RetireAsync(removed.Id, cancellationToken);
        }

        return result
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.Name, StringComparer.Ordinal)
            .ThenBy(option => option.Id)
            .ToArray();
    }
}
