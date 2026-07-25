using System.Net;
using System.Net.Http.Json;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Enums;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Authentication and permission enforcement over real HTTP against the real
/// database, including the shape of the error envelope the client depends on.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class FieldsApiAuthorizationTests
{
    private readonly SqlServerFixture _fixture;

    public FieldsApiAuthorizationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Anonymous_request_is_unauthorized()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/fields");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_user_with_view_permission_can_read_but_not_create()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);

        using HttpClient viewer = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.ViewerAEmail,
            TestData.OrganizationA.Id);

        HttpResponseMessage read = await viewer.GetAsync("/api/v1/fields");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        HttpResponseMessage write = await viewer.PostAsJsonAsync(
            "/api/v1/fields",
            NewFieldRequest("room_number"));

        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);

        Assert.Equal(
            0,
            await _fixture.CountRowsIgnoringSecurityAsync("[org].[Field]"));
    }

    [Fact]
    public async Task A_user_with_every_permission_can_run_the_full_lifecycle()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);

        using HttpClient administrator = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorAEmail,
            TestData.OrganizationA.Id);

        HttpResponseMessage created = await administrator.PostAsJsonAsync(
            "/api/v1/fields",
            NewFieldRequest("room_number"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        FieldDto field = await ReadFieldAsync(created);

        Assert.True(field.IsPrimaryDisplayField);

        HttpResponseMessage updated = await administrator.PutAsJsonAsync(
            $"/api/v1/fields/{field.Id}",
            new
            {
                name = "Room no.",
                isPrimaryDisplayField = true,
                rowVersion = field.RowVersion
            });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        FieldDto afterUpdate = await ReadFieldAsync(updated);
        Assert.Equal("Room no.", afterUpdate.Name);

        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/fields/{field.Id}")
        {
            Content = JsonContent.Create(new { rowVersion = afterUpdate.RowVersion })
        };

        HttpResponseMessage deleted = await administrator.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

        HttpResponseMessage afterDelete = await administrator.GetAsync(
            $"/api/v1/fields/{field.Id}");

        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task A_stale_row_version_is_reported_as_a_conflict()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);

        using HttpClient administrator = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorAEmail,
            TestData.OrganizationA.Id);

        FieldDto field = await ReadFieldAsync(
            await administrator.PostAsJsonAsync(
                "/api/v1/fields",
                NewFieldRequest("room_number")));

        object update = new
        {
            name = "Room no.",
            isPrimaryDisplayField = true,
            rowVersion = field.RowVersion
        };

        Assert.Equal(
            HttpStatusCode.OK,
            (await administrator.PutAsJsonAsync(
                $"/api/v1/fields/{field.Id}",
                update)).StatusCode);

        HttpResponseMessage conflict = await administrator.PutAsJsonAsync(
            $"/api/v1/fields/{field.Id}",
            update);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(
            MessageCode.Error.ConcurrencyConflict,
            await ReadMessageKeyAsync(conflict));
    }

    [Fact]
    public async Task A_field_of_another_organization_is_reported_as_missing()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);

        using HttpClient fromA = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorAEmail,
            TestData.OrganizationA.Id);

        FieldDto field = await ReadFieldAsync(
            await fromA.PostAsJsonAsync("/api/v1/fields", NewFieldRequest("room_number")));

        using HttpClient fromB = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorBEmail,
            TestData.OrganizationB.Id);

        HttpResponseMessage response = await fromB.GetAsync($"/api/v1/fields/{field.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(MessageCode.Error.NotFound, await ReadMessageKeyAsync(response));
    }

    [Fact]
    public async Task A_token_cannot_be_issued_for_an_organization_the_user_is_not_in()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/token",
            new
            {
                email = TestData.Users.AdministratorAEmail,
                password = TestData.Password,
                organizationId = TestData.OrganizationB.Id
            });

        await AssertStatusAsync(factory, response, HttpStatusCode.Forbidden);
        Assert.Equal(
            MessageCode.Error.OrganizationContextMissing,
            await ReadMessageKeyAsync(response));
    }

    [Fact]
    public async Task An_organization_header_that_disagrees_with_the_token_is_refused()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);

        using HttpClient administrator = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorAEmail,
            TestData.OrganizationA.Id);

        administrator.DefaultRequestHeaders.Add(
            "X-Organization-Id",
            TestData.OrganizationB.Id.ToString());

        HttpResponseMessage response = await administrator.GetAsync("/api/v1/fields");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_payload_is_reported_with_field_errors()
    {
        await _fixture.ResetOrgDataAsync();
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);

        using HttpClient administrator = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorAEmail,
            TestData.OrganizationA.Id);

        HttpResponseMessage response = await administrator.PostAsJsonAsync(
            "/api/v1/fields",
            new
            {
                targetEntityType = "Invoice",
                key = "  ",
                name = "",
                fieldTypeId = (int)EFieldType.Text
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ErrorEnvelope error =
            await response.Content.ReadFromJsonAsync<ErrorEnvelope>(FieldsApiFactory.Json)
            ?? throw new InvalidOperationException("Empty error body.");

        Assert.False(error.Success);
        Assert.Equal(MessageCode.Error.ValidationFailed, error.MessageKey);
        Assert.NotNull(error.FieldErrors);
        Assert.Contains(
            error.FieldErrors,
            fieldError => fieldError.Field == "targetEntityType");
        Assert.Contains(error.FieldErrors, fieldError => fieldError.Field == "key");
        Assert.Contains(error.FieldErrors, fieldError => fieldError.Field == "name");
    }

    /// <summary>
    /// Compares the status and, when it differs, reports the response body and
    /// anything the server logged, so an unexpected 500 is diagnosable.
    /// </summary>
    private static async Task AssertStatusAsync(
        FieldsApiFactory factory,
        HttpResponseMessage response,
        HttpStatusCode expected)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        Assert.Fail(
            $"Expected {expected} but the server answered {(int)response.StatusCode}. " +
            $"Body: {await response.Content.ReadAsStringAsync()}{Environment.NewLine}" +
            string.Join(Environment.NewLine, factory.ServerErrors));
    }

    private static object NewFieldRequest(string key)
    {
        return new
        {
            targetEntityType = TestData.TargetEntityType.Room,
            key,
            name = key,
            fieldTypeId = (int)EFieldType.Text
        };
    }

    private static async Task<FieldDto> ReadFieldAsync(HttpResponseMessage response)
    {
        FieldEnvelope envelope =
            await response.Content.ReadFromJsonAsync<FieldEnvelope>(FieldsApiFactory.Json)
            ?? throw new InvalidOperationException("Empty response body.");

        return envelope.Data;
    }

    private static async Task<string> ReadMessageKeyAsync(HttpResponseMessage response)
    {
        ErrorEnvelope error =
            await response.Content.ReadFromJsonAsync<ErrorEnvelope>(FieldsApiFactory.Json)
            ?? throw new InvalidOperationException("Empty error body.");

        return error.MessageKey;
    }

    private sealed record FieldEnvelope(bool Success, string MessageKey, FieldDto Data);

    private sealed record ErrorEnvelope(
        bool Success,
        string MessageKey,
        IReadOnlyList<FieldErrorPayload>? FieldErrors);

    private sealed record FieldErrorPayload(string Field, string MessageKey);
}
