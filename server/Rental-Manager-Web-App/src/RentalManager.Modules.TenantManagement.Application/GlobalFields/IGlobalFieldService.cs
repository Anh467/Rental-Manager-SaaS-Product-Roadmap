using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields;

public sealed record GetGlobalFieldsRequest
{
    public bool? IsActive { get; init; }
    public int? FieldTypeId { get; init; }
    public int? PageNumber { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}

public sealed record UpdateGlobalFieldStatusRequest(bool IsActive, string? RowVersion);

public interface IGlobalFieldService
{
    Task<PagedResult<FieldDto>> GetFieldsAsync(GetGlobalFieldsRequest request, CancellationToken cancellationToken = default);
    Task<FieldDto> GetFieldAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FieldDto> CreateFieldAsync(CreateFieldRequest request, CancellationToken cancellationToken = default);
    Task<FieldDto> UpdateFieldAsync(Guid id, UpdateFieldRequest request, CancellationToken cancellationToken = default);
    Task<FieldDto> UpdateStatusAsync(Guid id, UpdateGlobalFieldStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteFieldAsync(Guid id, DeleteFieldRequest request, CancellationToken cancellationToken = default);
}
