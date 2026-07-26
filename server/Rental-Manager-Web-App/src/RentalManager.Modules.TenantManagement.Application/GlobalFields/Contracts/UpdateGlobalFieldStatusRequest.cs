namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Contracts;

public sealed record UpdateGlobalFieldStatusRequest(bool IsActive, string? RowVersion);
