namespace RentalManager.BuildingBlocks.Contracts.Security;

/// <summary>
/// Alias for <see cref="ISecurityEventPublisher"/> used by audit-oriented
/// producers (activate/inactivate). SCRUM-84 owns durable audit persistence;
/// until then the same structured-event sink is used.
/// </summary>
public interface IAuditEventPublisher : ISecurityEventPublisher;
