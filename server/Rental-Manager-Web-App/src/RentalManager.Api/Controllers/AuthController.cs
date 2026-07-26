using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Contracts;
using RentalManager.Api.Security;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Api.Controllers;

public sealed record TokenRequest(
    string? Email,
    string? Password,
    Guid? OrganizationId);

public sealed record TokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPermissionReader _permissionReader;
    private readonly OrganizationContextAccessor _organizationContextAccessor;
    private readonly JwtTokenIssuer _tokenIssuer;

    public AuthController(
        IUserRepository users,
        IPermissionReader permissionReader,
        OrganizationContextAccessor organizationContextAccessor,
        JwtTokenIssuer tokenIssuer)
    {
        _users = users;
        _permissionReader = permissionReader;
        _organizationContextAccessor = organizationContextAccessor;
        _tokenIssuer = tokenIssuer;
    }

    /// <summary>
    /// Exchanges credentials plus a chosen organization for a token bound to that
    /// organization. Membership is resolved from
    /// <c>[org].[OrganizationUser]</c>, so one user may belong to many
    /// organizations and pick which one to enter.
    /// </summary>
    [HttpPost("token")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> IssueTokenAsync(
        [FromBody] TokenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.OrganizationId is not Guid organizationId ||
            organizationId == Guid.Empty)
        {
            throw new ValidationFailedException(
                nameof(TokenRequest.Email),
                MessageCode.Error.ValidationFailed);
        }

        User user = await _users.FindByNormalizedEmailAsync(
            request.Email.Trim().ToUpperInvariant(),
            cancellationToken)
            ?? throw new AuthenticationFailedException();

        if (!user.IsActive ||
            !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new AuthenticationFailedException();
        }

        // Bind the organization before the membership query so RLS only reveals
        // a membership row for an organization the user actually belongs to.
        _organizationContextAccessor.SetOrganization(organizationId);
        _organizationContextAccessor.SetUser(user.Id);

        bool isMember = await _permissionReader.IsActiveMemberAsync(
            user.Id,
            cancellationToken);

        if (!isMember)
        {
            throw new MissingOrganizationContextException();
        }

        (string token, DateTimeOffset expiresAt) = _tokenIssuer.Issue(
            user.Id,
            organizationId);

        return Ok(ApiResponse<TokenResponse>.Create(
            new TokenResponse(token, "Bearer", expiresAt),
            MessageCode.Success.SignedIn));
    }
}
