namespace RentalManager.Modules.TenantManagement.Application.Models.Dtos;

/// <summary>
/// Field as returned to clients. Deliberately not the domain entity, and
/// deliberately without <c>OrganizationId</c>: the organization is implied by the
/// authenticated context and is never part of the wire contract.
/// </summary>
public sealed record FieldDto
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required int FieldTypeId { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Base64 row version the client must echo back on update and delete.</summary>
    public required string RowVersion { get; init; }

    public required IReadOnlyList<FieldOptionDto> Options { get; init; }
}

public sealed record FieldOptionDto
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required int DisplayOrder { get; init; }

    public required bool IsActive { get; init; }
}
