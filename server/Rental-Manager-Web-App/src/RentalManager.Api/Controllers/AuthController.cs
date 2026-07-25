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
    /// organization. The user carries its organization and role assignment;
    /// row-level security scopes the role lookup to the selected organization.
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

        // Bind the context before resolving the organization role so RLS fails
        // closed when the requested organization does not match the assignment.
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
