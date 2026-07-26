using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields;

public sealed class GlobalFieldService(
    ISqlSession session,
    IFieldRepository fields,
    IGlobalFieldOptionRepository options,
    IFieldTypeRepository fieldTypes) : IGlobalFieldService
{
    public async Task<PagedResult<FieldDto>> GetFieldsAsync(GetGlobalFieldsRequest request, CancellationToken cancellationToken = default)
    {
        var page = await fields.GetPagedAsync(request.IsActive, request.FieldTypeId,
            new PagedRequest(request.PageNumber ?? request.Page, request.PageSize, request.Search, request.SortBy, request.SortDirection),
            cancellationToken);
        var stored = await options.GetByFieldIdsAsync(page.Items.Select(x => x.Id).ToArray(), cancellationToken);
        return page.Map(field => Map(field, stored.Where(x => x.FieldId == field.Id).ToArray()));
    }

    public async Task<FieldDto> GetFieldAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Field field = await fields.GetAsync(id, cancellationToken) ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);
        return Map(field, await options.GetByFieldIdAsync(id, cancellationToken));
    }

    public async Task<FieldDto> CreateFieldAsync(CreateFieldRequest request, CancellationToken cancellationToken = default)
    {
        FieldValidator.ValidateCreate(request);
        if (await fieldTypes.GetAsync(request.FieldTypeId, cancellationToken) is null)
        {
            throw new ValidationFailedException(
                nameof(CreateFieldRequest.FieldTypeId),
                MessageCode.Error.FieldTypeMismatch);
        }

        await using var transaction = await session.BeginTransactionAsync(cancellationToken);
        if (await fields.FindByKeyAsync(request.Key!, cancellationToken) is not null)
            throw new DuplicateResourceException(FieldInvariants.ObjectName, "key");
        var field = new Field { Id = Guid.CreateVersion7(), Key = request.Key!, Name = request.Name!.Trim(),
            Description = Clean(request.Description), FieldTypeId = request.FieldTypeId, IsActive = request.IsActive };
        await fields.InsertAsync(field, cancellationToken);
        var created = await ReplaceOptions(field, request.Options, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(field, created);
    }

    public async Task<FieldDto> UpdateFieldAsync(Guid id, UpdateFieldRequest request, CancellationToken cancellationToken = default)
    {
        Field field = await fields.GetAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);
        FieldValidator.ValidateUpdate(request, field.FieldTypeId);

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

        field.Name = request.Name!.Trim();
        field.Description = Clean(request.Description);
        field.IsActive = request.IsActive;

        await using var transaction = await session.BeginTransactionAsync(cancellationToken);
        await fields.UpdateAsync(
            field,
            FieldValidator.ParseRowVersion(request.RowVersion),
            cancellationToken);
        var replaced = await ReplaceOptions(field, request.Options, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(field, replaced);
    }

    public async Task<FieldDto> UpdateStatusAsync(Guid id, UpdateGlobalFieldStatusRequest request, CancellationToken cancellationToken = default)
    {
        var field = await fields.GetAsync(id, cancellationToken) ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);
        field.IsActive = request.IsActive;
        await fields.UpdateAsync(field, FieldValidator.ParseRowVersion(request.RowVersion), cancellationToken);
        return Map(field, await options.GetByFieldIdAsync(id, cancellationToken));
    }

    public async Task DeleteFieldAsync(Guid id, DeleteFieldRequest request, CancellationToken cancellationToken = default)
    {
        FieldValidator.ValidateDelete(request);
        await using var transaction = await session.BeginTransactionAsync(cancellationToken);
        _ = await fields.GetAsync(id, cancellationToken) ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);
        await fields.SoftDeleteAsync(id, FieldValidator.ParseRowVersion(request.RowVersion), cancellationToken);
        foreach (var option in await options.GetByFieldIdAsync(id, cancellationToken)) await options.RetireAsync(option.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<FieldOption>> ReplaceOptions(Field field, IReadOnlyList<FieldOptionInput>? inputs, CancellationToken ct)
    {
        var existing = await options.GetByFieldIdAsync(field.Id, ct);
        if (!FieldInvariants.RequiresOptions(field.FieldTypeId)) { foreach (var item in existing) await options.RetireAsync(item.Id, ct); return []; }
        var result = new List<FieldOption>(); var retained = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in inputs ?? [])
        {
            retained.Add(input.Key!);
            var item = existing.FirstOrDefault(x => x.Key == input.Key);
            if (item is null) { item = new FieldOption { Id = Guid.CreateVersion7(), FieldId = field.Id, Key = input.Key!, Name = input.Name!.Trim(), Description = Clean(input.Description), DisplayOrder = input.DisplayOrder, IsActive = input.IsActive }; await options.InsertAsync(item, ct); }
            else { item.Name = input.Name!.Trim(); item.Description = Clean(input.Description); item.DisplayOrder = input.DisplayOrder; item.IsActive = input.IsActive; await options.UpdateAsync(item, item.RowVersion, ct); }
            result.Add(item);
        }
        foreach (var item in existing.Where(x => !retained.Contains(x.Key))) await options.RetireAsync(item.Id, ct);
        return result.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToArray();
    }

    private static FieldDto Map(Field field, IReadOnlyList<FieldOption> items) => new()
    {
        Id = field.Id, Key = field.Key, Name = field.Name, Description = field.Description, FieldTypeId = field.FieldTypeId,
        IsActive = field.IsActive, CreatedAt = field.CreatedAt, UpdatedAt = field.UpdatedAt, RowVersion = Convert.ToBase64String(field.RowVersion),
        Options = items.Select(x => new FieldOptionDto { Id = x.Id, Key = x.Key, Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive }).ToArray()
    };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
