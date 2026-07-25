using System.Security.Cryptography;

namespace RentalManager.Api.Security;

/// <summary>
/// PBKDF2 password hashing using only the base class library, so no identity
/// framework and no extra package is pulled in.
/// </summary>
public static class PasswordHasher
{
    private const int SaltByteLength = 16;
    private const int HashByteLength = 32;
    private const int Iterations = 210_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static (string Hash, string Salt) Create(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltByteLength);
        byte[] hash = Derive(password, salt);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, string expectedHash, string salt)
    {
        if (string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(expectedHash) ||
            string.IsNullOrEmpty(salt))
        {
            return false;
        }

        byte[] saltBytes;
        byte[] expectedHashBytes;

        try
        {
            saltBytes = Convert.FromBase64String(salt);
            expectedHashBytes = Convert.FromBase64String(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualHash = Derive(password, saltBytes);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHashBytes);
    }

    private static byte[] Derive(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            HashByteLength);
    }
}
