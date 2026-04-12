namespace Nac.Persistence.Outbox;

/// <summary>
/// Persisted integration event waiting to be published to the event bus.
/// Written in the same DB transaction as business data; processed by a background worker.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Fully-qualified CLR type name for routing on the consumer side.</summary>
    public required string EventType { get; init; }

    /// <summary>JSON-serialized event payload.</summary>
    public required string Payload { get; init; }

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Set by the background worker once the event is successfully published.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Last error message from a failed publish attempt.</summary>
    public string? Error { get; set; }

    /// <summary>Number of delivery attempts made so far.</summary>
    public int RetryCount { get; set; }
}
