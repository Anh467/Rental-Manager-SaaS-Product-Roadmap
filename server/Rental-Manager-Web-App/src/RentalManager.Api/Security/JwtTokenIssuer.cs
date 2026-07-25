using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace RentalManager.Api.Security;

public sealed class JwtTokenIssuer
{
    private readonly JwtOptions _options;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <summary>
    /// Issues a token bound to one organization. The organization is part of the
    /// signed payload, which is what lets the API trust it later.
    /// </summary>
    public (string Token, DateTimeOffset ExpiresAt) Issue(
        Guid userId,
        Guid organizationId)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow
            .AddMinutes(_options.LifetimeMinutes);

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtClaimNames.Subject] = userId.ToString(),
                [JwtClaimNames.OrganizationId] = organizationId.ToString()
            }
        };

        var handler = new JsonWebTokenHandler
        {
            SetDefaultTimesOnTokenCreation = true
        };

        return (handler.CreateToken(descriptor), expiresAt);
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.NameIdentifier
        };
    }
}
