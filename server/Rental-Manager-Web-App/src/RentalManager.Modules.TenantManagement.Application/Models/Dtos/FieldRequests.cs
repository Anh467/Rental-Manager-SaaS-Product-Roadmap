using System.ComponentModel.DataAnnotations;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Validation;

namespace RentalManager.Modules.TenantManagement.Application.Models.Dtos;

public sealed record FieldOptionInput
{
    [Required]
    [MaxLength(DefinitionConstants.InlineTextMaxLength)]
    [DefinitionKey]
    public string? Key { get; init; }

    [Required]
    [MaxLength(DefinitionConstants.InlineTextMaxLength)]
    public string? Name { get; init; }

    [MaxLength(DefinitionConstants.TextAreaMaxLength)]
    public string? Description { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateFieldRequest
{
    [Required]
    [MaxLength(DefinitionConstants.InlineTextMaxLength)]
    [DefinitionKey]
    public string? Key { get; init; }

    // Length / whitespace-only checks live in FieldValidator so semantic
    // failures map to 422 rather than ASP.NET ModelState 400.
    // AllowEmptyStrings: .NET Required treats whitespace as invalid by default,
    // which would short-circuit as ModelState 400 before FieldValidator runs.
    [Required(AllowEmptyStrings = true)]
    public string? Name { get; init; }

    public string? Description { get; init; }

    public int FieldTypeId { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<FieldOptionInput>? Options { get; init; }
}

public sealed record UpdateFieldRequest
{
    [Required]
    [MaxLength(DefinitionConstants.InlineTextMaxLength)]
    public string? Name { get; init; }

    [MaxLength(DefinitionConstants.TextAreaMaxLength)]
    public string? Description { get; init; }

    /// <summary>
    /// Optional and immutable. Present only so an attempt to change it can be
    /// rejected explicitly rather than silently ignored.
    /// </summary>
    [MaxLength(DefinitionConstants.InlineTextMaxLength)]
    [DefinitionKey]
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
