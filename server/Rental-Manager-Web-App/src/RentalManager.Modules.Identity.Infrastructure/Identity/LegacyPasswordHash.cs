using System.Security.Cryptography;

namespace RentalManager.Modules.Identity.Infrastructure.Identity;

/// <summary>
/// Creates legacy PBKDF2 hashes for seeding and migration tests.
/// Runtime verification goes through <see cref="LegacyCompatiblePasswordHasher"/>.
/// </summary>
public static class LegacyPasswordHash
{
    private const int SaltByteLength = 16;
    private const int HashByteLength = 32;
    private const int Iterations = 210_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static (string Hash, string Salt) Create(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltByteLength);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            HashByteLength);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }
}
