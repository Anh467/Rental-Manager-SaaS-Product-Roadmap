using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using RentalManager.Api.Http;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;
using JsonException = System.Text.Json.JsonException;

namespace RentalManager.Api.UnitTests.Http;

public sealed class ApiExceptionMapperTests
{
    private readonly ApiExceptionMapper _mapper = new();

    [Fact]
    public void Antiforgery_validation_failure_maps_to_400_ERR_001()
    {
        ApiErrorMapping mapping = _mapper.Map(new AntiforgeryValidationException("bad token"));

        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.ValidationFailed, mapping.MessageKey);
    }

    [Fact]
    public void Malformed_json_maps_to_400_ERR_001()
    {
        ApiErrorMapping mapping = _mapper.Map(new JsonException("unexpected token"));

        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.ValidationFailed, mapping.MessageKey);
    }

    [Fact]
    public void Bad_http_request_maps_to_400_ERR_001()
    {
        ApiErrorMapping mapping = _mapper.Map(new BadHttpRequestException("malformed request"));

        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.ValidationFailed, mapping.MessageKey);
    }

    [Fact]
    public void Field_validation_failure_maps_to_422_with_field_errors()
    {
        var exception = new ValidationFailedException(
            [
                new ValidationFailure("key", MessageCode.Error.ValidationFailed),
                new ValidationFailure("name", MessageCode.Error.ValidationFailed)
            ]);

        ApiErrorMapping mapping = _mapper.Map(exception);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.ValidationFailed, mapping.MessageKey);
        Assert.NotNull(mapping.FieldErrors);
        Assert.Equal(2, mapping.FieldErrors!.Count);
        Assert.Contains(mapping.FieldErrors, error => error.FieldKey == "key");
        Assert.Contains(mapping.FieldErrors, error => error.FieldKey == "name");
    }

    [Fact]
    public void Authentication_failure_maps_to_401_ERR_003()
    {
        ApiErrorMapping mapping = _mapper.Map(new AuthenticationFailedException());

        Assert.Equal(StatusCodes.Status401Unauthorized, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.AuthenticationRequired, mapping.MessageKey);
    }

    [Fact]
    public void Permission_denied_maps_to_403_ERR_004()
    {
        ApiErrorMapping mapping = _mapper.Map(new PermissionDeniedException("update", "field"));

        Assert.Equal(StatusCodes.Status403Forbidden, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.PermissionDenied, mapping.MessageKey);
    }

    [Fact]
    public void Missing_organization_context_maps_to_403_ERR_005()
    {
        ApiErrorMapping mapping = _mapper.Map(new MissingOrganizationContextException());

        Assert.Equal(StatusCodes.Status403Forbidden, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.OrganizationContextMissing, mapping.MessageKey);
    }

    [Fact]
    public void Resource_not_found_maps_to_404_ERR_002()
    {
        ApiErrorMapping mapping = _mapper.Map(new ResourceNotFoundException("field"));

        Assert.Equal(StatusCodes.Status404NotFound, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.NotFound, mapping.MessageKey);
    }

    [Fact]
    public void Duplicate_resource_maps_to_409_ERR_007()
    {
        ApiErrorMapping mapping = _mapper.Map(new DuplicateResourceException("field", "key"));

        Assert.Equal(StatusCodes.Status409Conflict, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.AlreadyExists, mapping.MessageKey);
        Assert.NotNull(mapping.FieldErrors);
        Assert.Single(mapping.FieldErrors!);
    }

    [Fact]
    public void Concurrency_conflict_maps_to_409_ERR_010()
    {
        ApiErrorMapping mapping = _mapper.Map(new ConcurrencyConflictException("field"));

        Assert.Equal(StatusCodes.Status409Conflict, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.ConcurrencyConflict, mapping.MessageKey);
    }

    [Fact]
    public void Business_rule_with_an_active_key_maps_to_409_with_that_key()
    {
        ApiErrorMapping mapping = _mapper.Map(
            new BusinessRuleException(MessageCode.Error.ImmutableProperty));

        Assert.Equal(StatusCodes.Status409Conflict, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.ImmutableProperty, mapping.MessageKey);
    }

    [Fact]
    public void Business_rule_with_a_deprecated_key_falls_back_to_500_ERR_050()
    {
        // ERR-034 is deprecated; a business rule that still carries it is a
        // programming error and must never reach the client as a 409.
        ApiErrorMapping mapping = _mapper.Map(new BusinessRuleException("ERR-034"));

        Assert.Equal(StatusCodes.Status500InternalServerError, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.UnexpectedError, mapping.MessageKey);
    }

    [Fact]
    public void External_service_unavailable_maps_to_503_ERR_049_with_service_parameter()
    {
        ApiErrorMapping mapping = _mapper.Map(
            new ExternalServiceUnavailableException("objectStorage"));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.ServiceUnavailable, mapping.MessageKey);
        Assert.NotNull(mapping.Parameters);
        Assert.Equal("objectStorage", mapping.Parameters![MessageCode.Parameter.Service]);
    }

    [Fact]
    public void Unmapped_domain_exception_maps_to_500_ERR_050_not_409()
    {
        var exception = new UnmappedTestDomainException();

        ApiErrorMapping mapping = _mapper.Map(exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.UnexpectedError, mapping.MessageKey);
        Assert.NotEqual(StatusCodes.Status409Conflict, mapping.StatusCode);
    }

    [Fact]
    public void Any_other_exception_maps_to_500_ERR_050()
    {
        ApiErrorMapping mapping = _mapper.Map(new InvalidOperationException("boom"));

        Assert.Equal(StatusCodes.Status500InternalServerError, mapping.StatusCode);
        Assert.Equal(MessageCode.Error.UnexpectedError, mapping.MessageKey);
    }

    [Fact]
    public void Every_mapped_key_is_active_in_the_catalog()
    {
        Exception[] exceptions =
        [
            new AntiforgeryValidationException("bad token"),
            new JsonException("bad json"),
            new BadHttpRequestException("malformed"),
            new ValidationFailedException("key", MessageCode.Error.ValidationFailed),
            new AuthenticationFailedException(),
            new PermissionDeniedException("update", "field"),
            new MissingOrganizationContextException(),
            new ResourceNotFoundException("field"),
            new DuplicateResourceException("field"),
            new ConcurrencyConflictException("field"),
            new BusinessRuleException(MessageCode.Error.ImmutableProperty),
            new BusinessRuleException("ERR-034"),
            new ExternalServiceUnavailableException("objectStorage"),
            new UnmappedTestDomainException(),
            new InvalidOperationException("boom")
        ];

        foreach (Exception exception in exceptions)
        {
            ApiErrorMapping mapping = _mapper.Map(exception);
            Assert.True(
                MessageCatalog.IsActive(mapping.MessageKey),
                $"{exception.GetType().Name} mapped to non-active key {mapping.MessageKey}.");
        }
    }

    private sealed class UnmappedTestDomainException()
        : DomainException("ERR-999-not-a-real-key");
}
