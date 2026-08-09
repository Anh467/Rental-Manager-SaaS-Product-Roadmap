using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RentalManager.Modules.Identity.Infrastructure;
using RentalManager.Modules.Identity.Infrastructure.Persistence;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Boots the real API against the test database, so authentication, the
/// organization context middleware, permission policies and the error envelope
/// are all the production ones.
/// </summary>
internal class FieldsApiFactory : WebApplicationFactory<Program>
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
    /// Signs in with cookie auth and returns a client that sends cookies plus
    /// the CSRF header on unsafe requests.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email,
        Guid organizationId)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        await AttachCsrfHeaderAsync(client);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email,
                password = TestData.Password
            });

        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Signing in as '{email}' failed with {(int)loginResponse.StatusCode}: " +
                await loginResponse.Content.ReadAsStringAsync() +
                Environment.NewLine +
                string.Join(Environment.NewLine, ServerErrors));
        }

        LoginEnvelope? envelope =
            await loginResponse.Content.ReadFromJsonAsync<LoginEnvelope>(Json);

        if (envelope?.Data is not null &&
            string.Equals(
                envelope.Data.Status,
                "organizationSelectionRequired",
                StringComparison.Ordinal))
        {
            await AttachCsrfHeaderAsync(client);

            HttpResponseMessage selectResponse = await client.PostAsJsonAsync(
                "/api/v1/auth/select-organization",
                new
                {
                    selectionTicket = envelope.Data.SelectionTicket,
                    organizationId
                });

            if (!selectResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Selecting organization for '{email}' failed with " +
                    $"{(int)selectResponse.StatusCode}: " +
                    await selectResponse.Content.ReadAsStringAsync() +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, ServerErrors));
            }
        }

        await AttachCsrfHeaderAsync(client);
        return client;
    }

    public IReadOnlyList<string> ServerErrors => _serverErrors;

    private readonly List<string> _serverErrors = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
            logging.AddProvider(new CapturingLoggerProvider(_serverErrors)));

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Enabled"] = "false",
                ["BootstrapAdmin:Email"] = "",
                ["BootstrapAdmin:Password"] = "",
                ["DataProtection:KeyRingPath"] = Path.Combine(
                    Path.GetTempPath(),
                    "rental-manager-tests-dp-" + Guid.NewGuid().ToString("N"))
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISqlConnectionFactory>();
            services.AddSingleton<ISqlConnectionFactory>(
                new SqlConnectionFactory(_connectionString));

            services.RemoveAll<IIdentityConnectionFactory>();
            services.AddSingleton<IIdentityConnectionFactory>(
                new IdentityConnectionFactory(_connectionString));
        });
    }

    internal static async Task AttachCsrfHeaderAsync(HttpClient client)
    {
        HttpResponseMessage csrfResponse = await client.GetAsync("/api/v1/auth/csrf");
        csrfResponse.EnsureSuccessStatusCode();

        CsrfEnvelope envelope =
            await csrfResponse.Content.ReadFromJsonAsync<CsrfEnvelope>(Json)
            ?? throw new InvalidOperationException("CSRF endpoint returned an empty body.");

        client.DefaultRequestHeaders.Remove(
            IdentityInfrastructureServiceCollectionExtensions.AntiforgeryHeaderName);
        client.DefaultRequestHeaders.Add(
            IdentityInfrastructureServiceCollectionExtensions.AntiforgeryHeaderName,
            envelope.Data.RequestToken);
    }

    internal sealed record CsrfEnvelope(bool Success, string MessageKey, CsrfPayload Data);

    internal sealed record CsrfPayload(string RequestToken);

    internal sealed record LoginEnvelope(bool Success, string MessageKey, LoginPayload? Data);

    internal sealed record LoginPayload(
        string? Status,
        string? SelectionTicket);

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
