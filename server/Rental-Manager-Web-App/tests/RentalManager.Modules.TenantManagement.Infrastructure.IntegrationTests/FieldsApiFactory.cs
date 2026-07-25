using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Boots the real API against the test database, so authentication, the
/// organization context middleware, permission policies and the error envelope
/// are all the production ones.
/// </summary>
internal sealed class FieldsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public FieldsApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Signs in and returns a client whose every request carries the resulting
    /// bearer token, which is the only way the organization reaches the server.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email,
        Guid organizationId)
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/token",
            new
            {
                email,
                password = TestData.Password,
                organizationId
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Signing in as '{email}' failed with {(int)response.StatusCode}: " +
                await response.Content.ReadAsStringAsync() +
                Environment.NewLine +
                string.Join(Environment.NewLine, ServerErrors));
        }

        TokenEnvelope envelope =
            await response.Content.ReadFromJsonAsync<TokenEnvelope>(Json)
            ?? throw new InvalidOperationException(
                "The token endpoint returned an empty body.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            envelope.Data.AccessToken);

        return client;
    }

    /// <summary>
    /// Server-side failures logged so far. A 500 from the host is otherwise
    /// invisible to the test, which makes such a failure very hard to diagnose.
    /// </summary>
    public IReadOnlyList<string> ServerErrors => _serverErrors;

    private readonly List<string> _serverErrors = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
            logging.AddProvider(new CapturingLoggerProvider(_serverErrors)));

        // The connection string is read from configuration while services are
        // registered, which is before a test can add a configuration source. The
        // connection factory is therefore replaced directly instead. Everything
        // else, including the JWT settings, comes from the application's own
        // Development configuration.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISqlConnectionFactory>();
            services.AddSingleton<ISqlConnectionFactory>(
                new SqlConnectionFactory(_connectionString));
        });
    }

    internal sealed record TokenEnvelope(bool Success, string MessageKey, TokenPayload Data);

    internal sealed record TokenPayload(string AccessToken, string TokenType);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _sink;

        public CapturingLoggerProvider(List<string> sink)
        {
            _sink = sink;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CapturingLogger(_sink);
        }

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly List<string> _sink;

            public CapturingLogger(List<string> sink)
            {
                _sink = sink;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= LogLevel.Error;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                lock (_sink)
                {
                    _sink.Add($"{formatter(state, exception)} {exception}");
                }
            }
        }
    }
}
