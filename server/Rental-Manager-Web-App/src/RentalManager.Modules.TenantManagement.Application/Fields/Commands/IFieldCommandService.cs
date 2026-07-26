using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

public interface IFieldCommandService
{
    Task<FieldDto> CreateFieldAsync(
        CreateFieldRequest request,
        CancellationToken cancellationToken = default);

    Task<FieldDto> UpdateFieldAsync(
        Guid id,
        UpdateFieldRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteFieldAsync(
        Guid id,
        DeleteFieldRequest request,
        CancellationToken cancellationToken = default);
}
