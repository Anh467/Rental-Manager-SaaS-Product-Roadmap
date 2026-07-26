using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests.Fakes;

internal sealed class FakeFieldTypeRepository : IFieldTypeRepository
{
    private readonly Dictionary<int, FieldType> _types = [];

    public FakeFieldTypeRepository(bool seedCatalogue = true)
    {
        if (!seedCatalogue)
        {
            return;
        }

        foreach (EFieldType fieldType in Enum.GetValues<EFieldType>())
        {
            int id = (int)fieldType;
            _types[id] = new FieldType
            {
                Id = id,
                Key = fieldType.ToString().ToLowerInvariant(),
                Name = fieldType.ToString()
            };
        }
    }

    public void Clear() => _types.Clear();

    public void Seed(FieldType fieldType) => _types[fieldType.Id] = fieldType;

    public Task<FieldType?> GetAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _types.TryGetValue(id, out FieldType? fieldType) ? fieldType : null);
    }

    public Task<IReadOnlyCollection<FieldType>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<FieldType>>(_types.Values.ToArray());
    }

    public Task<byte[]?> InsertAsync(
        FieldType entity,
        CancellationToken cancellationToken = default)
    {
        _types[entity.Id] = entity;
        return Task.FromResult<byte[]?>(null);
    }

    public Task<byte[]?> UpdateAsync(
        FieldType entity,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        _types[entity.Id] = entity;
        return Task.FromResult<byte[]?>(null);
    }

    public Task SaveAsync(
        FieldType entity,
        CancellationToken cancellationToken = default)
    {
        _types[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        FieldType entity,
        CancellationToken cancellationToken = default)
    {
        _types.Remove(entity.Id);
        return Task.CompletedTask;
    }
}
