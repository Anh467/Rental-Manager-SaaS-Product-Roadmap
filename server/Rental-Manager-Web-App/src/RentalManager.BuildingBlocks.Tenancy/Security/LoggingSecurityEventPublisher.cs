using Microsoft.Extensions.Logging;
using RentalManager.BuildingBlocks.Contracts.Security;

namespace RentalManager.BuildingBlocks.Tenancy.Security;

/// <summary>
/// Default sink: emits the event as a structured log entry and nothing else.
/// There is deliberately no durable store here — SCRUM-84 owns AuditLog /
/// SecurityLog persistence, retention and querying, and will add its own
/// implementation of <see cref="ISecurityEventPublisher"/>.
/// </summary>
public sealed class LoggingSecurityEventPublisher : ISecurityEventPublisher
{
    private readonly ILogger<LoggingSecurityEventPublisher> _logger;

    public LoggingSecurityEventPublisher(ILogger<LoggingSecurityEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);

        _logger.LogInformation(
            "Security event {SecurityEventType} correlationId={CorrelationId} data={SecurityEventData}",
            securityEvent.EventType,
            securityEvent.CorrelationId,
            Describe(securityEvent.Data));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Only the producer-supplied safe fields are rendered, in a stable order,
    /// so a log line never depends on an object's <c>ToString</c> surprising us.
    /// </summary>
    private static string Describe(IReadOnlyDictionary<string, object?> data) =>
        data.Count == 0
            ? "{}"
            : string.Join(
                ", ",
                data
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}={entry.Value}"));
}
