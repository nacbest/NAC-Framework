namespace Nac.Abstractions.Messaging;

/// <summary>
/// Handler for integration events received from the event bus.
/// Implementations must be idempotent — the same event may be delivered more than once.
/// </summary>
/// <typeparam name="TEvent">The integration event type to handle.</typeparam>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    /// <summary>Handles the integration event. Must be idempotent.</summary>
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
