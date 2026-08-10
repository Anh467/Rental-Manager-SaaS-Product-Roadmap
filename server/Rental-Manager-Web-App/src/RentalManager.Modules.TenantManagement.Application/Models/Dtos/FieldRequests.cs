using System.ComponentModel.DataAnnotations;

namespace RentalManager.Modules.TenantManagement.Application.Models.Dtos;

/// <summary>
/// Semantic length/format rules live in <c>FieldValidator</c> so they map to
/// HTTP 422. Keep this DTO free of DataAnnotations MaxLength/Required so ASP.NET
/// ModelState is reserved for malformed JSON and binding failures (HTTP 400).
/// </summary>
public sealed record FieldOptionInput
{
    public string? Key { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateFieldRequest
{
    // Missing basic request fields stay ModelState → 400. Format/length/business
    // rules are enforced in FieldValidator → 422.
    [Required]
    public string? Key { get; init; }

    // AllowEmptyStrings: whitespace-only names must reach FieldValidator (422)
    // instead of short-circuiting as ModelState 400.
    [Required(AllowEmptyStrings = true)]
    public string? Name { get; init; }

    public string? Description { get; init; }

    public int FieldTypeId { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<FieldOptionInput>? Options { get; init; }
}

/// <summary>
/// No DataAnnotations semantic constraints: Update validation is entirely in
/// FieldValidator so whitespace/overlong/nested option failures are 422.
/// </summary>
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

    public bool IsActive { get; init; } = true;

    public string? RowVersion { get; init; }

    public IReadOnlyList<FieldOptionInput>? Options { get; init; }
}

public sealed record DeleteFieldRequest
{
    public string? RowVersion { get; init; }
}

public sealed record GetFieldsRequest
{
    public bool? IsActive { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
