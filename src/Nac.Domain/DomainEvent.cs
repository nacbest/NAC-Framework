using Nac.Messaging;

namespace Nac.Domain;

/// <summary>
/// Base record for domain events. Domain events are in-process notifications
/// dispatched via the Mediator after UnitOfWork commit.
/// </summary>
/// <remarks>
/// Domain events are internal to a module. For cross-module communication,
/// use <see cref="IntegrationEvent"/> via <see cref="IEventBus"/>.
/// </remarks>
public abstract record DomainEvent : INotification
{
    /// <summary>Unique identifier for this event instance.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the event was raised.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
