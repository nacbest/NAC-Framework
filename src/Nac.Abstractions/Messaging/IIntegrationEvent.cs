namespace Nac.Abstractions.Messaging;

/// <summary>
/// Marker interface for integration events that cross module boundaries.
/// Integration events are published via <see cref="IEventBus"/> and must be serializable.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique identifier for this event instance, used for idempotency/deduplication.</summary>
    Guid EventId { get; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>Fully qualified type name for deserialization routing.</summary>
    string EventType { get; }
}

/// <summary>
/// Base record for integration events. Provides default implementations for
/// <see cref="IIntegrationEvent.EventId"/>, <see cref="IIntegrationEvent.OccurredAt"/>,
/// and <see cref="IIntegrationEvent.EventType"/>.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => GetType().FullName!;
}
