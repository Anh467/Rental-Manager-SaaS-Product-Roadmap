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

        FieldsApiFactory.AttachExternalIdentityHeaders(
            client,
            TestData.Users.AdministratorASubject);

        await FieldsApiFactory.AttachCsrfHeaderAsync(client);

        HttpResponseMessage login = await client.PostAsync(
            "/api/v1/auth/login",
            content: null);

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

    /// <summary>
    /// The retired contract must not merely be ignored: an email and password in
    /// the body can never establish a session.
    /// </summary>
    [Fact]
    public async Task Email_and_password_body_cannot_sign_in()
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
                password = "any-password"
            });

        Assert.NotEqual(HttpStatusCode.OK, login.StatusCode);
        Assert.DoesNotContain(
            login.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies)
                ? cookies
                : [],
            cookie => cookie.Contains("rentalmanager.auth", StringComparison.OrdinalIgnoreCase));

        HttpResponseMessage me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Unknown_subject_is_rejected()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        // No email means first-login provisioning cannot proceed, so an unknown
        // subject is refused instead of silently creating an account.
        FieldsApiFactory.AttachExternalIdentityHeaders(client, "unknown-subject");
        await FieldsApiFactory.AttachCsrfHeaderAsync(client);

        HttpResponseMessage login = await client.PostAsync(
            "/api/v1/auth/login",
            content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, login.StatusCode);

        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.Equal("ERR-001", body.RootElement.GetProperty("messageKey").GetString());
    }

    /// <summary>
    /// The provider's subject is the identity key, so a subject differing only in
    /// case is a different identity and must not resolve to the seeded user.
    /// </summary>
    [Fact]
    public async Task Subject_comparison_is_case_sensitive()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        FieldsApiFactory.AttachExternalIdentityHeaders(
            client,
            TestData.Users.AdministratorASubject.ToUpperInvariant());

        await FieldsApiFactory.AttachCsrfHeaderAsync(client);

        HttpResponseMessage login = await client.PostAsync(
            "/api/v1/auth/login",
            content: null);

        Assert.NotEqual(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Mutation_without_csrf_is_rejected()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = await factory.CreateAuthenticatedClientAsync(
            TestData.Users.AdministratorASubject,
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
            TestData.Users.AdministratorASubject,
            TestData.OrganizationA.Id);

        HttpResponseMessage logout = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        using var logoutBody = JsonDocument.Parse(await logout.Content.ReadAsStringAsync());
        Assert.Equal(
            "SCS-018",
            logoutBody.RootElement.GetProperty("messageKey").GetString());

        HttpResponseMessage me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
