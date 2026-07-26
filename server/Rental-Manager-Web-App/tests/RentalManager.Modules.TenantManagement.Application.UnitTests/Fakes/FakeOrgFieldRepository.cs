using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests.Fakes;

/// <summary>
/// Hand-written in-memory field store. Written by hand rather than with a
/// mocking library so the test project needs no extra package.
/// </summary>
internal sealed class FakeOrgFieldRepository : IOrgFieldRepository
{
    private readonly Dictionary<Guid, Field> _fields = [];

    private static long _rowVersionCounter;

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
        if (!_fields.TryGetValue(entity.Id, out Field? stored) ||
            stored.DeletedAt is not null)
        {
            throw new ResourceNotFoundException("field");
        }

        if (expectedRowVersion is null ||
            !stored.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new ConcurrencyConflictException("field");
        }

        entity.UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1);
        entity.CreatedAt = stored.CreatedAt;
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
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        Field[] matching = _fields.Values
            .Where(field => field.DeletedAt is null)
            .Where(field => isActive is null || field.IsActive == isActive)
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ThenBy(field => field.Id)
            .ToArray();

        int skip = request.Offset > int.MaxValue
            ? int.MaxValue
            : (int)request.Offset;

        return Task.FromResult(new PagedResult<Field>(
            matching.Skip(skip).Take(request.PageSize).ToArray(),
            request.Page,
            request.PageSize,
            matching.Length));
    }

    public Task<Field?> FindByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_fields.Values.FirstOrDefault(field =>
            string.Equals(field.Key, key, StringComparison.Ordinal)));
    }

    /// <summary>
    /// SQL <c>rowversion</c> is always 8 bytes; the fake mirrors that length so
    /// row-version validation round-trips correctly.
    /// </summary>
    private static byte[] NextRowVersion()
    {
        return BitConverter.GetBytes(Interlocked.Increment(ref _rowVersionCounter));
    }
}
