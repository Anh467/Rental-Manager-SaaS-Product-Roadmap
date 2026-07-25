using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Auditing;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields;

/// <summary>
/// Orchestrates field use cases. The transaction boundary is opened here rather
/// than inside a repository, so it is visible at the layer that decides what
/// belongs in one atomic change.
/// </summary>
public sealed class FieldService : IFieldService
{
    private static readonly TimeSpan ScopeLockTimeout = TimeSpan.FromSeconds(5);
    private const string FieldEntityType = "Field";

    private readonly ISqlSession _session;
    private readonly ISqlApplicationLock _applicationLock;
    private readonly IOrgFieldRepository _fields;
    private readonly IFieldOptionRepository _fieldOptions;
    private readonly IAuditLogWriter _auditLog;
    private readonly IOrganizationContext _organizationContext;

    public FieldService(
        ISqlSession session,
        ISqlApplicationLock applicationLock,
        IOrgFieldRepository fields,
        IFieldOptionRepository fieldOptions,
        IAuditLogWriter auditLog,
        IOrganizationContext organizationContext)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(applicationLock);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldOptions);
        ArgumentNullException.ThrowIfNull(auditLog);
        ArgumentNullException.ThrowIfNull(organizationContext);

        _session = session;
        _applicationLock = applicationLock;
        _fields = fields;
        _fieldOptions = fieldOptions;
        _auditLog = auditLog;
        _organizationContext = organizationContext;
    }

    public async Task<PagedResult<FieldDto>> GetFieldsAsync(
        GetFieldsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TargetEntityType is not null &&
            !FieldInvariants.IsSupportedTargetEntityType(request.TargetEntityType))
        {
            throw new ValidationFailedException(
                nameof(GetFieldsRequest.TargetEntityType),
                MessageCode.Error.ValidationFailed);
        }

        var pagedRequest = new PagedRequest(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDirection);

        PagedResult<Field> page = await _fields.GetPagedAsync(
            request.TargetEntityType,
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

        string targetEntityType = request.TargetEntityType!;
        string key = FieldKeyNormalizer.Trim(request.Key);
        string normalizedKey = FieldKeyNormalizer.Normalize(request.Key);

        await using ISqlTransactionScope transaction =
            await _session.BeginTransactionAsync(cancellationToken);

        await AcquireScopeLockAsync(targetEntityType, cancellationToken);

        Field? conflicting = await _fields.FindByNormalizedKeyAsync(
            targetEntityType,
            normalizedKey,
            cancellationToken);

        if (conflicting is not null)
        {
            throw new DuplicateResourceException(FieldInvariants.ObjectName);
        }

        PrimaryTransition transition = await ResolveCreateTransitionAsync(
            targetEntityType,
            request.IsActive,
            request.IsPrimaryDisplayField,
            cancellationToken);

        var field = new Field
        {
            Id = Guid.CreateVersion7(),
            TargetEntityType = targetEntityType,
            Key = key,
            NormalizedKey = normalizedKey,
            Name = request.Name!.Trim(),
            Description = NormalizeDescription(request.Description),
            FieldTypeId = request.FieldTypeId,
            IsRequired = request.IsRequired,
            IsPrimaryDisplayField = transition.ResolvedIsPrimary,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        if (transition.DemoteFieldId is Guid demoteId)
        {
            await _fields.ClearPrimaryAsync(demoteId, cancellationToken);
        }

        await _fields.InsertAsync(field, cancellationToken);

        IReadOnlyList<FieldOption> options = await ReplaceOptionsAsync(
            field,
            request.Options,
            cancellationToken);

        await WriteAuditAsync(field, AuditAction.Created, cancellationToken);

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

        await AcquireScopeLockAsync(field.TargetEntityType, cancellationToken);

        PrimaryTransition transition = await ResolveUpdateTransitionAsync(
            field,
            request.IsActive,
            request.IsPrimaryDisplayField,
            request.ReplacementPrimaryFieldId,
            cancellationToken);

        field.Name = request.Name!.Trim();
        field.Description = NormalizeDescription(request.Description);
        field.IsRequired = request.IsRequired;
        field.IsActive = request.IsActive;
        field.DisplayOrder = request.DisplayOrder;
        field.IsPrimaryDisplayField = transition.ResolvedIsPrimary;

        if (transition.DemoteFieldId is Guid demoteId)
        {
            await _fields.ClearPrimaryAsync(demoteId, cancellationToken);
        }

        await _fields.UpdateAsync(field, expectedRowVersion, cancellationToken);

        // Promotion runs after this field has given up the primary flag, so the
        // unique filtered index never sees two active primaries.
        if (transition.PromoteFieldId is Guid promoteId)
        {
            await _fields.SetPrimaryAsync(promoteId, cancellationToken);
        }

        IReadOnlyList<FieldOption> options = await ReplaceOptionsAsync(
            field,
            request.Options,
            cancellationToken);

        await WriteAuditAsync(field, AuditAction.Updated, cancellationToken);

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

        Field field = await _fields.GetAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        await AcquireScopeLockAsync(field.TargetEntityType, cancellationToken);

        PrimaryTransition transition = await ResolveUpdateTransitionAsync(
            field,
            isActive: false,
            requestedIsPrimary: false,
            request.ReplacementPrimaryFieldId,
            cancellationToken);

        await _fields.SoftDeleteAsync(id, expectedRowVersion, cancellationToken);

        foreach (FieldOption option in
                 await _fieldOptions.GetByFieldIdAsync(id, cancellationToken))
        {
            await _fieldOptions.RetireAsync(option.Id, cancellationToken);
        }

        if (transition.PromoteFieldId is Guid promoteId)
        {
            await _fields.SetPrimaryAsync(promoteId, cancellationToken);
        }

        await WriteAuditAsync(field, AuditAction.Deleted, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AcquireScopeLockAsync(
        string targetEntityType,
        CancellationToken cancellationToken)
    {
        Guid organizationId = _organizationContext.OrganizationId
            ?? throw new MissingOrganizationContextException();

        // A scope with no field yet has no row to lock, so the invariant is
        // coordinated by an application lock instead of a range lock.
        await _applicationLock.AcquireAsync(
            FieldInvariants.LockResourceName(organizationId, targetEntityType),
            FieldInvariants.ObjectName,
            ScopeLockTimeout,
            cancellationToken);
    }

    private static void EnsureImmutablePropertiesUnchanged(
        Field field,
        UpdateFieldRequest request)
    {
        if (request.Key is not null &&
            !string.Equals(
                FieldKeyNormalizer.Normalize(request.Key),
                field.NormalizedKey,
                StringComparison.Ordinal))
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

    /// <summary>
    /// The first active field in a scope becomes primary automatically, and an
    /// explicit request to be primary demotes the incumbent.
    /// </summary>
    private async Task<PrimaryTransition> ResolveCreateTransitionAsync(
        string targetEntityType,
        bool isActive,
        bool requestedIsPrimary,
        CancellationToken cancellationToken)
    {
        if (!isActive)
        {
            return new PrimaryTransition(false, null, null);
        }

        Field? currentPrimary = await _fields.GetActivePrimaryAsync(
            targetEntityType,
            cancellationToken);

        if (currentPrimary is null)
        {
            return new PrimaryTransition(true, null, null);
        }

        return requestedIsPrimary
            ? new PrimaryTransition(true, currentPrimary.Id, null)
            : new PrimaryTransition(false, null, null);
    }

    /// <summary>
    /// Resolves what happens to the primary display field when this field is
    /// updated or deleted.
    /// </summary>
    private async Task<PrimaryTransition> ResolveUpdateTransitionAsync(
        Field field,
        bool isActive,
        bool requestedIsPrimary,
        Guid? replacementPrimaryFieldId,
        CancellationToken cancellationToken)
    {
        bool wasPrimary = field is { IsPrimaryDisplayField: true, IsActive: true };
        bool wantsPrimary = isActive && requestedIsPrimary;

        if (wasPrimary && !wantsPrimary)
        {
            bool hasOtherActiveFields = await _fields.HasOtherActiveFieldsAsync(
                field.TargetEntityType,
                field.Id,
                cancellationToken);

            if (!hasOtherActiveFields)
            {
                // Nothing else can hold the flag. If the field stays active the
                // scope must keep exactly one primary, so it keeps it.
                return new PrimaryTransition(isActive, null, null);
            }

            Guid replacementId = await ResolveReplacementAsync(
                field,
                replacementPrimaryFieldId,
                cancellationToken);

            return new PrimaryTransition(false, null, replacementId);
        }

        if (wantsPrimary)
        {
            Field? currentPrimary = await _fields.GetActivePrimaryAsync(
                field.TargetEntityType,
                cancellationToken);

            Guid? demoteId = currentPrimary is not null && currentPrimary.Id != field.Id
                ? currentPrimary.Id
                : null;

            return new PrimaryTransition(true, demoteId, null);
        }

        if (isActive)
        {
            Field? currentPrimary = await _fields.GetActivePrimaryAsync(
                field.TargetEntityType,
                cancellationToken);

            if (currentPrimary is null || currentPrimary.Id == field.Id)
            {
                return new PrimaryTransition(true, null, null);
            }
        }

        return new PrimaryTransition(false, null, null);
    }

    private async Task<Guid> ResolveReplacementAsync(
        Field field,
        Guid? replacementPrimaryFieldId,
        CancellationToken cancellationToken)
    {
        if (replacementPrimaryFieldId is not Guid replacementId)
        {
            throw new BusinessRuleException(
                MessageCode.Error.LastActivePrimaryFieldRemoval,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Object] = FieldInvariants.ObjectName
                });
        }

        Field? replacement = await _fields.GetAsync(replacementId, cancellationToken);

        bool isEligible =
            replacement is not null &&
            replacement.Id != field.Id &&
            replacement.IsActive &&
            string.Equals(
                replacement.TargetEntityType,
                field.TargetEntityType,
                StringComparison.Ordinal);

        if (!isEligible)
        {
            throw new BusinessRuleException(
                MessageCode.Error.LastActivePrimaryFieldRemoval,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Object] = FieldInvariants.ObjectName
                });
        }

        return replacementId;
    }

    /// <summary>
    /// Brings the stored options in line with the request inside the caller's
    /// transaction. An option key is immutable, so a renamed key is expressed as
    /// retiring one option and adding another.
    /// </summary>
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
            option => option.NormalizedKey,
            StringComparer.Ordinal);

        var result = new List<FieldOption>();
        var keptKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (FieldOptionInput input in inputs ?? [])
        {
            string key = FieldKeyNormalizer.Trim(input.Key);
            string normalizedKey = FieldKeyNormalizer.Normalize(input.Key);
            keptKeys.Add(normalizedKey);

            if (storedByKey.TryGetValue(normalizedKey, out FieldOption? existing))
            {
                existing.Name = input.Name!.Trim();
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
                NormalizedKey = normalizedKey,
                Name = input.Name!.Trim(),
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive
            };

            await _fieldOptions.InsertAsync(option, cancellationToken);
            result.Add(option);
        }

        foreach (FieldOption removed in stored.Where(
                     option => !keptKeys.Contains(option.NormalizedKey)))
        {
            await _fieldOptions.RetireAsync(removed.Id, cancellationToken);
        }

        return result
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.Name, StringComparer.Ordinal)
            .ThenBy(option => option.Id)
            .ToArray();
    }

    private async Task WriteAuditAsync(
        Field field,
        string action,
        CancellationToken cancellationToken)
    {
        await _auditLog.WriteAsync(
            new AuditLogEntry(
                FieldEntityType,
                field.Id,
                action,
                FieldInvariants.DescribeConfiguration(
                    field.Name,
                    field.IsRequired,
                    field.IsActive,
                    field.IsPrimaryDisplayField)),
            cancellationToken);
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
            TargetEntityType = field.TargetEntityType,
            Key = field.Key,
            Name = field.Name,
            Description = field.Description,
            FieldTypeId = field.FieldTypeId,
            IsRequired = field.IsRequired,
            IsPrimaryDisplayField = field.IsPrimaryDisplayField,
            IsActive = field.IsActive,
            DisplayOrder = field.DisplayOrder,
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
                    DisplayOrder = option.DisplayOrder,
                    IsActive = option.IsActive
                })
                .ToArray()
        };
    }

    /// <summary>
    /// Decisions about the primary display field, resolved before any write so
    /// the writes can be ordered to keep the unique filtered index satisfied.
    /// </summary>
    private sealed record PrimaryTransition(
        bool ResolvedIsPrimary,
        Guid? DemoteFieldId,
        Guid? PromoteFieldId);
}
