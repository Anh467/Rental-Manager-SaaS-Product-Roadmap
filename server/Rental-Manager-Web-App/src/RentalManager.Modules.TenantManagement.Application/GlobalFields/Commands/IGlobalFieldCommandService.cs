using RentalManager.Modules.TenantManagement.Application.GlobalFields.Contracts;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public interface IGlobalFieldCommandService
{
    Task<FieldDto> CreateFieldAsync(
        CreateFieldRequest request,
        CancellationToken cancellationToken = default);

    Task<FieldDto> UpdateFieldAsync(
        Guid id,
        UpdateFieldRequest request,
        CancellationToken cancellationToken = default);

    Task<FieldDto> UpdateStatusAsync(
        Guid id,
        UpdateGlobalFieldStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteFieldAsync(
        Guid id,
        DeleteFieldRequest request,
        CancellationToken cancellationToken = default);
}
