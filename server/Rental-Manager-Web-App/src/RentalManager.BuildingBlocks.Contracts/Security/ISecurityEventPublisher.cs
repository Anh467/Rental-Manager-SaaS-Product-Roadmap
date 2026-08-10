namespace RentalManager.BuildingBlocks.Contracts.Security;

/// <summary>
/// The single way any module reports a security-relevant occurrence. This is
/// the publishing boundary only: where the event ends up (structured log today,
/// durable store once SCRUM-84 lands) is the sink's concern, so producers never
/// need to change when the sink does.
/// </summary>
public interface ISecurityEventPublisher
{
    Task PublishAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
}
