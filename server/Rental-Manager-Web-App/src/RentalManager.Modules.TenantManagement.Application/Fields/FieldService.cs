using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields;

/// <summary>
/// Orchestrates organization field catalogue use cases. Field options are
/// persisted in the same transaction as their parent field.
/// </summary>
public sealed class FieldService : IFieldService
{
    private readonly ISqlSession _session;
    private readonly IOrgFieldRepository _fields;
    private readonly IFieldOptionRepository _fieldOptions;
    private readonly IFieldTypeRepository _fieldTypes;

    public FieldService(
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

    public async Task<PagedResult<FieldDto>> GetFieldsAsync(
        GetFieldsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pagedRequest = new PagedRequest(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDirection);

        PagedResult<Field> page = await _fields.GetPagedAsync(
            request.IsActive,
            pagedRequest,
            cancellationToken);

        Guid[] fieldIds = page.Items.Select(field => field.Id).ToArray();
        IReadOnlyList<FieldOption> options = await _fieldOptions.GetByFieldIdsAsync(
            fieldIds,
            cancellationToken);

        Dictionary<Guid, List<FieldOption>> optionsByField = options
            .GroupBy(option => option.FieldId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return page.Map(field => MapToDto(
            field,
            optionsByField.TryGetValue(field.Id, out List<FieldOption>? fieldOptions)
                ? fieldOptions
                : []));
    }

    public async Task<FieldDto> GetFieldAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Field field = await _fields.GetAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        IReadOnlyList<FieldOption> options = await _fieldOptions.GetByFieldIdAsync(
            field.Id,
            cancellationToken);

        return MapToDto(field, options);
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
            Description = NormalizeDescription(request.Description),
            FieldTypeId = request.FieldTypeId,
            IsActive = request.IsActive
        };

        await _fields.InsertAsync(field, cancellationToken);

        IReadOnlyList<FieldOption> options = await ReplaceOptionsAsync(
            field,
            request.Options,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return MapToDto(field, options);
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
        field.Description = NormalizeDescription(request.Description);
        field.IsActive = request.IsActive;

        await _fields.UpdateAsync(field, expectedRowVersion, cancellationToken);

        IReadOnlyList<FieldOption> options = await ReplaceOptionsAsync(
            field,
            request.Options,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return MapToDto(field, options);
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
                existing.Description = NormalizeDescription(input.Description);
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
                Description = NormalizeDescription(input.Description),
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

    private static string? NormalizeDescription(string? description)
    {
        string? trimmed = description?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static FieldDto MapToDto(
        Field field,
        IReadOnlyList<FieldOption> options)
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
}
