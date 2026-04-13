namespace Nac.Abstractions.Messaging;

/// <summary>
/// Abstraction for publishing integration events across module or service boundaries.
/// Implementations include InMemoryEventBus (development) and distributed brokers (RabbitMQ, Kafka).
/// Swapping between implementations requires only changing the composition root registration.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event. For distributed implementations,
    /// events should go through the Outbox pattern for transactional guarantees.
    /// </summary>
    Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default);
}
