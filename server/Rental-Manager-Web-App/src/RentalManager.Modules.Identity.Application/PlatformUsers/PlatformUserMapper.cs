using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.PlatformUsers;

internal static class PlatformUserMapper
{
    public static PlatformUserDto ToDto(PlatformUserRecord user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            Convert.ToBase64String(user.RowVersion));

    public static byte[] ParseRowVersion(string? rowVersion)
    {
        if (TryParseRowVersion(rowVersion, out byte[] parsed))
        {
            return parsed;
        }

        throw new ValidationFailedException(
            nameof(UpdatePlatformUserRequest.RowVersion),
            MessageCode.Error.ValidationFailed);
    }

    private const int SqlRowVersionLength = 8;

    private static bool TryParseRowVersion(string? rowVersion, out byte[] parsed)
    {
        parsed = [];

        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[SqlRowVersionLength];
        if (!Convert.TryFromBase64String(rowVersion, buffer, out int written) ||
            written != SqlRowVersionLength)
        {
            return false;
        }

        parsed = buffer.ToArray();
        return true;
    }
}
