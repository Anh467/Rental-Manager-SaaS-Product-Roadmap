using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class CookieAuthenticationTests
{
    private readonly SqlServerFixture _fixture;

    public CookieAuthenticationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Database_trustworthy_is_off()
    {
        await using SqlConnection connection = await _fixture.OpenConnectionAsync();
        int isTrustworthy = await connection.ExecuteScalarAsync<int>(
            """
            SELECT CAST(CASE WHEN DATABASEPROPERTYEX(DB_NAME(), 'IsTrustworthy') = 1
                THEN 1 ELSE 0 END AS INT);
            """);

        Assert.Equal(0, isTrustworthy);
    }

    [Fact]
    public async Task Login_sets_http_only_cookie_and_me_succeeds()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        await FieldsApiFactory.AttachCsrfHeaderAsync(client);

        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = TestData.Users.AdministratorAEmail,
                password = TestData.Password
            });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(
            login.Headers.GetValues("Set-Cookie"),
            cookie => cookie.Contains("rentalmanager.auth", StringComparison.OrdinalIgnoreCase) &&
                      cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
                      cookie.Contains("path=/", StringComparison.OrdinalIgnoreCase));

        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(loginBody.RootElement.GetProperty("data").TryGetProperty("accessToken", out _));
        Assert.False(loginBody.RootElement.GetProperty("data").TryGetProperty("refreshToken", out _));

        HttpResponseMessage me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Me_without_cookie_returns_401_json_not_redirect()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Null(me.Headers.Location);

        using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("ERR-003", body.RootElement.GetProperty("messageKey").GetString());
    }

    [Fact]
    public async Task Mutation_without_csrf_is_rejected()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorAEmail,
            TestData.OrganizationA.Id);

        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/fields",
            new
            {
                key = "csrf_blocked",
                name = "CSRF blocked",
                fieldTypeId = 1
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_invalidates_cookie_session()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorAEmail,
            TestData.OrganizationA.Id);

        HttpResponseMessage logout = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        HttpResponseMessage me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
