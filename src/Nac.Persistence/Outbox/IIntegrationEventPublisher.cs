namespace Nac.Persistence.Outbox;

/// <summary>
/// Abstraction over the message-broker transport used by <see cref="OutboxWorker"/> to
/// publish processed outbox events.
/// Register a concrete implementation (e.g. a MassTransit or Azure Service Bus bridge)
/// in the DI container. If no implementation is registered the worker logs a warning and
/// marks events as processed so the outbox table does not grow unbounded.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes a single integration event to the configured transport.
    /// </summary>
    /// <param name="eventType">
    /// The assembly-qualified type name originally stored in <see cref="OutboxEvent.EventType"/>.
    /// </param>
    /// <param name="payload">The JSON-serialised event payload.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task PublishAsync(string eventType, string payload, CancellationToken ct = default);
}
