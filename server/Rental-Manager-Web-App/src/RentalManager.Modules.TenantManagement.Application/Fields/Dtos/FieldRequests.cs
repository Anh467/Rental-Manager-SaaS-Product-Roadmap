namespace RentalManager.Modules.TenantManagement.Application.Fields.Dtos;

public sealed record FieldOptionInput
{
    public string? Key { get; init; }

    public string? Name { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateFieldRequest
{
    public string? TargetEntityType { get; init; }

    public string? Key { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int FieldTypeId { get; init; }

    public bool IsRequired { get; init; }

    public bool IsPrimaryDisplayField { get; init; }

    public bool IsActive { get; init; } = true;

    public int DisplayOrder { get; init; }

    public IReadOnlyList<FieldOptionInput>? Options { get; init; }
}

public sealed record UpdateFieldRequest
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// Optional and immutable. Present only so an attempt to change it can be
    /// rejected explicitly rather than silently ignored.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>Optional and immutable, for the same reason as <see cref="Key"/>.</summary>
    public int? FieldTypeId { get; init; }

    public bool IsRequired { get; init; }

    public bool IsPrimaryDisplayField { get; init; }

    public bool IsActive { get; init; } = true;

    public int DisplayOrder { get; init; }

    /// <summary>
    /// Field that takes over as primary when this change would otherwise leave
    /// the scope without one.
    /// </summary>
    public Guid? ReplacementPrimaryFieldId { get; init; }

    public string? RowVersion { get; init; }

    public IReadOnlyList<FieldOptionInput>? Options { get; init; }
}

public sealed record DeleteFieldRequest
{
    public Guid? ReplacementPrimaryFieldId { get; init; }

    public string? RowVersion { get; init; }
}

public sealed record GetFieldsRequest
{
    public string? TargetEntityType { get; init; }

    public bool? IsActive { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
