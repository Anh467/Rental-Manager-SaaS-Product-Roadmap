using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests.Fakes;

internal sealed class FakeFieldOptionRepository : IFieldOptionRepository
{
    private readonly Dictionary<Guid, FieldOption> _options = [];

    private static long _rowVersionCounter;

    public IReadOnlyCollection<FieldOption> All => _options.Values;

    public Task<FieldOption?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _options.TryGetValue(id, out FieldOption? option) && option.DeletedAt is null
                ? option
                : null);
    }

    public Task<IReadOnlyCollection<FieldOption>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<FieldOption>>(
            _options.Values.Where(option => option.DeletedAt is null).ToArray());
    }

    public Task<byte[]?> InsertAsync(
        FieldOption entity,
        CancellationToken cancellationToken = default)
    {
        bool duplicate = _options.Values.Any(option =>
            option.FieldId == entity.FieldId &&
            option.NormalizedKey == entity.NormalizedKey &&
            option.DeletedAt is null);

        if (duplicate)
        {
            throw new DuplicateResourceException("fieldOption");
        }

        entity.RowVersion = NextRowVersion();
        _options[entity.Id] = entity;

        return Task.FromResult<byte[]?>(entity.RowVersion);
    }

    public Task<byte[]?> UpdateAsync(
        FieldOption entity,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.TryGetValue(entity.Id, out FieldOption? stored))
        {
            throw new ResourceNotFoundException("fieldOption");
        }

        if (expectedRowVersion is null ||
            !stored.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new ConcurrencyConflictException("fieldOption");
        }

        entity.RowVersion = NextRowVersion();
        _options[entity.Id] = entity;

        return Task.FromResult<byte[]?>(entity.RowVersion);
    }

    public Task SaveAsync(
        FieldOption entity,
        CancellationToken cancellationToken = default)
    {
        _options[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        FieldOption entity,
        CancellationToken cancellationToken = default)
    {
        _options.Remove(entity.Id);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(
        Guid id,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        return RetireAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<FieldOption>> GetByFieldIdAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FieldOption>>(
            _options.Values
                .Where(option => option.FieldId == fieldId && option.DeletedAt is null)
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.Id)
                .ToArray());
    }

    public Task<IReadOnlyList<FieldOption>> GetByFieldIdsAsync(
        IReadOnlyCollection<Guid> fieldIds,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FieldOption>>(
            _options.Values
                .Where(option =>
                    fieldIds.Contains(option.FieldId) && option.DeletedAt is null)
                .ToArray());
    }

    public Task RetireAsync(
        Guid optionId,
        CancellationToken cancellationToken = default)
    {
        if (_options.TryGetValue(optionId, out FieldOption? option))
        {
            option.DeletedAt = DateTimeOffset.UnixEpoch;
            option.RowVersion = NextRowVersion();
        }

        return Task.CompletedTask;
    }

    private static byte[] NextRowVersion()
    {
        return BitConverter.GetBytes(Interlocked.Increment(ref _rowVersionCounter));
    }
}
