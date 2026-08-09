using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace RentalManager.Modules.Identity.Infrastructure.Identity;

/// <summary>
/// Verifies ASP.NET Identity hashes and legacy PBKDF2 (hash + salt columns).
/// New passwords are always hashed with the Identity format.
/// </summary>
public sealed class LegacyCompatiblePasswordHasher : PasswordHasher<ApplicationUser>
{
    private const string IdentityHashPrefix = "AQAAAA";
    private const int SaltByteLength = 16;
    private const int HashByteLength = 32;
    private const int Iterations = 210_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public override PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user,
        string hashedPassword,
        string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(hashedPassword) ||
            string.IsNullOrEmpty(providedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        if (hashedPassword.StartsWith(IdentityHashPrefix, StringComparison.Ordinal))
        {
            return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }

        if (string.IsNullOrEmpty(user.PasswordSalt))
        {
            return PasswordVerificationResult.Failed;
        }

        return TryVerifyLegacy(providedPassword, hashedPassword, user.PasswordSalt)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    private static bool TryVerifyLegacy(
        string password,
        string expectedHash,
        string salt)
    {
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

        if (saltBytes.Length == 0 || expectedHashBytes.Length == 0)
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            Algorithm,
            HashByteLength);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHashBytes);
    }
}
