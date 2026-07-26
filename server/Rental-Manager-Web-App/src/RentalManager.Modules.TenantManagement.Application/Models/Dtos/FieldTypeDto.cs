namespace RentalManager.Modules.TenantManagement.Application.Models.Dtos;

public sealed record FieldTypeDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    public string? Description { get; init; }
}
