using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class AuthRateLimitingTests
{
    private readonly SqlServerFixture _fixture;

    public AuthRateLimitingTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_and_token_are_rate_limited_with_err_040()
    {
        using var factory = new RateLimitedApiFactory(_fixture.ConnectionString, permitLimit: 2);
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < 2; i++)
        {
            HttpResponseMessage allowed = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = "nobody@example.com", password = "wrong-password" });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        HttpResponseMessage rejected = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "nobody@example.com", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        ErrorEnvelope? body = await rejected.Content.ReadFromJsonAsync<ErrorEnvelope>(
            FieldsApiFactory.Json);
        Assert.NotNull(body);
        Assert.Equal(MessageCode.Error.RateLimitExceeded, body.MessageKey);
        Assert.False(string.IsNullOrWhiteSpace(body.CorrelationId));
    }

    [Fact]
    public async Task Authenticated_field_endpoints_are_not_subject_to_auth_rate_limit()
    {
        using var factory = new RateLimitedApiFactory(_fixture.ConnectionString, permitLimit: 1);
        using HttpClient authenticated =
            await factory.CreateAuthenticatedClientAsync(
                TestData.Users.AdministratorAEmail,
                TestData.OrganizationA.Id);

        using HttpClient anonymous = factory.CreateClient();
        HttpResponseMessage limited = await anonymous.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "nobody@example.com", password = "wrong-password" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);

        HttpResponseMessage fields = await authenticated.GetAsync("/api/v1/fields");
        Assert.Equal(HttpStatusCode.OK, fields.StatusCode);
    }

    private sealed class RateLimitedApiFactory : FieldsApiFactory
    {
        private readonly int _permitLimit;

        public RateLimitedApiFactory(string connectionString, int permitLimit)
            : base(connectionString)
        {
            _permitLimit = permitLimit;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AuthRateLimiting:PermitLimit"] = _permitLimit.ToString(),
                    ["AuthRateLimiting:WindowSeconds"] = "60"
                });
            });
        }
    }

    private sealed record ErrorEnvelope(
        bool Success,
        string MessageKey,
        string CorrelationId);
}
