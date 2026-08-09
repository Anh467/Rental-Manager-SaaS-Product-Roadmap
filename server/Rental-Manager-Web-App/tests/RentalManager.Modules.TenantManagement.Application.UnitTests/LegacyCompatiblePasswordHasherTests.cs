using Microsoft.AspNetCore.Identity;
using RentalManager.Modules.Identity.Infrastructure.Identity;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class LegacyCompatiblePasswordHasherTests
{
    private readonly LegacyCompatiblePasswordHasher _hasher = new();

    [Fact]
    public void Identity_format_hash_verifies_and_legacy_wrong_password_fails()
    {
        var user = new ApplicationUser { Id = Guid.CreateVersion7() };
        string hash = _hasher.HashPassword(user, "ValidPassword1!");

        Assert.StartsWith("AQAAAA", hash, StringComparison.Ordinal);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(user, hash, "ValidPassword1!"));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            _hasher.VerifyHashedPassword(user, hash, "WrongPassword1!"));
    }

    [Fact]
    public void Legacy_pbkdf2_returns_success_rehash_needed()
    {
        (string hash, string salt) = LegacyPasswordHash.Create("LegacyPassword1!");
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            PasswordHash = hash,
            PasswordSalt = salt
        };

        Assert.Equal(
            PasswordVerificationResult.SuccessRehashNeeded,
            _hasher.VerifyHashedPassword(user, hash, "LegacyPassword1!"));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            _hasher.VerifyHashedPassword(user, hash, "WrongPassword1!"));
    }

    [Fact]
    public void Malformed_legacy_hash_fails_safely()
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            PasswordSalt = "not-base64!!!"
        };

        Assert.Equal(
            PasswordVerificationResult.Failed,
            _hasher.VerifyHashedPassword(user, "also-not-base64!!!", "any"));
    }
}
