namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Contracts;

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
