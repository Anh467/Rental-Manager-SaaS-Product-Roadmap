namespace RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;

public sealed record PlatformUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public sealed record GetPlatformUsersRequest
{
    public string? Search { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed record UpdatePlatformUserRequest
{
    public required string DisplayName { get; init; }

    public required string RowVersion { get; init; }
}

public sealed record UpdatePlatformUserStatusRequest
{
    public required bool IsActive { get; init; }

    public required string RowVersion { get; init; }
}
