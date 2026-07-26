using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

/// <summary>
/// Organization field catalogue mutations. Field options are persisted in the
/// same transaction as their parent field.
/// </summary>
public sealed class FieldCommandService : IFieldCommandService
{
    private readonly ISqlSession _session;
    private readonly IOrgFieldRepository _fields;
    private readonly IFieldOptionRepository _fieldOptions;
    private readonly IFieldTypeRepository _fieldTypes;

    public FieldCommandService(
        ISqlSession session,
        IOrgFieldRepository fields,
        IFieldOptionRepository fieldOptions,
        IFieldTypeRepository fieldTypes)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldOptions);
        ArgumentNullException.ThrowIfNull(fieldTypes);

        _session = session;
        _fields = fields;
        _fieldOptions = fieldOptions;
        _fieldTypes = fieldTypes;
    }

    public async Task<FieldDto> CreateFieldAsync(
        CreateFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        FieldValidator.ValidateCreate(request);
        if (await _fieldTypes.GetAsync(request.FieldTypeId, cancellationToken) is null)
        {
            throw new ValidationFailedException(
                nameof(CreateFieldRequest.FieldTypeId),
                MessageCode.Error.FieldTypeMismatch);
        }

        await using ISqlTransactionScope transaction =
            await _session.BeginTransactionAsync(cancellationToken);

        Field? conflicting = await _fields.FindByKeyAsync(
            request.Key!,
            cancellationToken);

        if (conflicting is not null)
        {
            throw new DuplicateResourceException(FieldInvariants.ObjectName);
        }

        var field = new Field
        {
            Id = Guid.CreateVersion7(),
            Key = request.Key!,
            Name = request.Name!.Trim(),
            Description = FieldDtoMapper.NormalizeDescription(request.Description),
            FieldTypeId = request.FieldTypeId,
            IsActive = request.IsActive
        };

        await _fields.InsertAsync(field, cancellationToken);

        IReadOnlyList<FieldOption> options = await ReplaceOptionsAsync(
            field,
            request.Options,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return FieldDtoMapper.ToDto(field, options);
    }

    public async Task<FieldDto> UpdateFieldAsync(
        Guid id,
        UpdateFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        byte[] expectedRowVersion = FieldValidator.ParseRowVersion(request.RowVersion);

        await using ISqlTransactionScope transaction =
            await _session.BeginTransactionAsync(cancellationToken);

        Field field = await _fields.GetAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        FieldValidator.ValidateUpdate(request, field.FieldTypeId);
        EnsureImmutablePropertiesUnchanged(field, request);

        field.Name = request.Name!.Trim();
        field.Description = FieldDtoMapper.NormalizeDescription(request.Description);
        field.IsActive = request.IsActive;

        await _fields.UpdateAsync(field, expectedRowVersion, cancellationToken);

        IReadOnlyList<FieldOption> options = await ReplaceOptionsAsync(
            field,
            request.Options,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return FieldDtoMapper.ToDto(field, options);
    }

    public async Task DeleteFieldAsync(
        Guid id,
        DeleteFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        FieldValidator.ValidateDelete(request);
        byte[] expectedRowVersion = FieldValidator.ParseRowVersion(request.RowVersion);

        await using ISqlTransactionScope transaction =
            await _session.BeginTransactionAsync(cancellationToken);

        _ = await _fields.GetAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        await _fields.SoftDeleteAsync(id, expectedRowVersion, cancellationToken);

        foreach (FieldOption option in
                 await _fieldOptions.GetByFieldIdAsync(id, cancellationToken))
        {
            await _fieldOptions.RetireAsync(option.Id, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static void EnsureImmutablePropertiesUnchanged(
        Field field,
        UpdateFieldRequest request)
    {
        if (request.Key is not null &&
            !string.Equals(request.Key, field.Key, StringComparison.Ordinal))
        {
            throw new BusinessRuleException(
                MessageCode.Error.ImmutableProperty,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Field] = nameof(Field.Key)
                });
        }

        if (request.FieldTypeId is int requestedFieldTypeId &&
            requestedFieldTypeId != field.FieldTypeId)
        {
            throw new BusinessRuleException(
                MessageCode.Error.ImmutableProperty,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Field] = nameof(Field.FieldTypeId)
                });
        }
    }

    private async Task<IReadOnlyList<FieldOption>> ReplaceOptionsAsync(
        Field field,
        IReadOnlyList<FieldOptionInput>? inputs,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FieldOption> stored = await _fieldOptions.GetByFieldIdAsync(
            field.Id,
            cancellationToken);

        if (!FieldInvariants.RequiresOptions(field.FieldTypeId))
        {
            foreach (FieldOption orphan in stored)
            {
                await _fieldOptions.RetireAsync(orphan.Id, cancellationToken);
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

                await _fieldOptions.UpdateAsync(
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

            await _fieldOptions.InsertAsync(option, cancellationToken);
            result.Add(option);
        }

        foreach (FieldOption removed in stored.Where(
                     option => !keptKeys.Contains(option.Key)))
        {
            await _fieldOptions.RetireAsync(removed.Id, cancellationToken);
        }

        return result
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.Name, StringComparer.Ordinal)
            .ThenBy(option => option.Id)
            .ToArray();
    }
}
