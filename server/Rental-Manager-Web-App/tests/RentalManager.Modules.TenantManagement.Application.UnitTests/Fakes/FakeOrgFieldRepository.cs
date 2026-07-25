using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests.Fakes;

/// <summary>
/// Hand-written in-memory field store. Written by hand rather than with a
/// mocking library so the test project needs no extra package, and so the fake
/// can enforce the same primary-field uniqueness the database enforces.
/// </summary>
internal sealed class FakeOrgFieldRepository : IOrgFieldRepository
{
    private readonly Dictionary<Guid, Field> _fields = [];

    public List<string> WriteLog { get; } = [];

    public IReadOnlyCollection<Field> All => _fields.Values;

    public void Seed(Field field)
    {
        field.RowVersion = NextRowVersion();
        _fields[field.Id] = field;
    }

    public Field Require(Guid id) => _fields[id];

    public Task<Field?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Field? field = _fields.TryGetValue(id, out Field? found) &&
                       found.DeletedAt is null
            ? found
            : null;

        return Task.FromResult(field);
    }

    public Task<IReadOnlyCollection<Field>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Field>>(
            _fields.Values.Where(field => field.DeletedAt is null).ToArray());
    }

    public Task<byte[]?> InsertAsync(
        Field entity,
        CancellationToken cancellationToken = default)
    {
        EnsureSinglePrimary(entity);

        entity.CreatedAt = DateTimeOffset.UnixEpoch;
        entity.UpdatedAt = DateTimeOffset.UnixEpoch;
        entity.RowVersion = NextRowVersion();
        _fields[entity.Id] = entity;

        WriteLog.Add($"Insert:{entity.Id}");

        return Task.FromResult<byte[]?>(entity.RowVersion);
    }

    public Task<byte[]?> UpdateAsync(
        Field entity,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (!_fields.TryGetValue(entity.Id, out Field? stored))
        {
            throw new ResourceNotFoundException("field");
        }

        if (expectedRowVersion is null ||
            !stored.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new ConcurrencyConflictException("field");
        }

        EnsureSinglePrimary(entity);

        entity.RowVersion = NextRowVersion();
        _fields[entity.Id] = entity;

        WriteLog.Add($"Update:{entity.Id}");

        return Task.FromResult<byte[]?>(entity.RowVersion);
    }

    public Task SaveAsync(Field entity, CancellationToken cancellationToken = default)
    {
        _fields[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Field entity, CancellationToken cancellationToken = default)
    {
        _fields.Remove(entity.Id);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(
        Guid id,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (!_fields.TryGetValue(id, out Field? stored) || stored.DeletedAt is not null)
        {
            throw new ResourceNotFoundException("field");
        }

        if (expectedRowVersion is null ||
            !stored.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new ConcurrencyConflictException("field");
        }

        stored.DeletedAt = DateTimeOffset.UnixEpoch;
        stored.RowVersion = NextRowVersion();

        WriteLog.Add($"SoftDelete:{id}");

        return Task.CompletedTask;
    }

    public Task<PagedResult<Field>> GetPagedAsync(
        string? targetEntityType,
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        Field[] matching = _fields.Values
            .Where(field => field.DeletedAt is null)
            .Where(field =>
                targetEntityType is null ||
                field.TargetEntityType == targetEntityType)
            .Where(field => isActive is null || field.IsActive == isActive)
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.Name, StringComparer.Ordinal)
            .ThenBy(field => field.Id)
            .ToArray();

        return Task.FromResult(new PagedResult<Field>(
            matching.Skip(request.Offset).Take(request.PageSize).ToArray(),
            request.Page,
            request.PageSize,
            matching.Length));
    }

    public Task<Field?> FindByNormalizedKeyAsync(
        string targetEntityType,
        string normalizedKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_fields.Values.FirstOrDefault(field =>
            field.TargetEntityType == targetEntityType &&
            field.NormalizedKey == normalizedKey));
    }

    public Task<Field?> GetActivePrimaryAsync(
        string targetEntityType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_fields.Values.FirstOrDefault(field =>
            field.TargetEntityType == targetEntityType &&
            field is { IsPrimaryDisplayField: true, IsActive: true, DeletedAt: null }));
    }

    public Task<bool> HasOtherActiveFieldsAsync(
        string targetEntityType,
        Guid excludedFieldId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_fields.Values.Any(field =>
            field.TargetEntityType == targetEntityType &&
            field.Id != excludedFieldId &&
            field is { IsActive: true, DeletedAt: null }));
    }

    public Task ClearPrimaryAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        if (_fields.TryGetValue(fieldId, out Field? field))
        {
            field.IsPrimaryDisplayField = false;
            field.RowVersion = NextRowVersion();
        }

        WriteLog.Add($"ClearPrimary:{fieldId}");

        return Task.CompletedTask;
    }

    public Task SetPrimaryAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        if (!_fields.TryGetValue(fieldId, out Field? field) ||
            !field.IsActive ||
            field.DeletedAt is not null)
        {
            throw new BusinessRuleException("ERR-016");
        }

        field.IsPrimaryDisplayField = true;
        field.RowVersion = NextRowVersion();

        WriteLog.Add($"SetPrimary:{fieldId}");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mirrors the unique filtered index, so a service bug that would produce two
    /// active primaries fails in unit tests too.
    /// </summary>
    private void EnsureSinglePrimary(Field candidate)
    {
        if (candidate is not { IsPrimaryDisplayField: true, IsActive: true, DeletedAt: null })
        {
            return;
        }

        bool conflict = _fields.Values.Any(field =>
            field.Id != candidate.Id &&
            field.TargetEntityType == candidate.TargetEntityType &&
            field is { IsPrimaryDisplayField: true, IsActive: true, DeletedAt: null });

        if (conflict)
        {
            throw new BusinessRuleException("ERR-015");
        }
    }

    private static byte[] NextRowVersion()
    {
        return BitConverter.GetBytes(Interlocked.Increment(ref _rowVersionCounter));
    }

    private static long _rowVersionCounter;
}
