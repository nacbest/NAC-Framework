namespace Nac.Persistence.Outbox;

/// <summary>
/// Tracks which integration events have been processed on the consumer side.
/// Used for idempotency — prevents duplicate processing when the same event is delivered more than once.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>The EventId from <see cref="Nac.Abstractions.Messaging.IIntegrationEvent"/>.</summary>
    public Guid EventId { get; init; }

    /// <summary>Fully-qualified CLR type name of the event.</summary>
    public required string EventType { get; init; }

    /// <summary>When the event was successfully processed.</summary>
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}
