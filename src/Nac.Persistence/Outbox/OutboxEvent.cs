namespace Nac.Persistence.Outbox;

/// <summary>
/// Persistent record of an integration event waiting to be published to the message broker.
/// Rows are inserted in the same database transaction as the business entity changes,
/// guaranteeing at-least-once delivery via the transactional outbox pattern.
/// </summary>
public sealed class OutboxEvent
{
    /// <summary>Gets the unique identifier of this outbox record.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the assembly-qualified type name of the original integration event, used by the
    /// publisher to deserialise the <see cref="Payload"/> back to the concrete event type.
    /// </summary>
    public string EventType { get; init; } = default!;

    /// <summary>Gets the JSON-serialised integration event payload.</summary>
    public string Payload { get; init; } = default!;

    /// <summary>Gets the UTC timestamp when this record was written to the outbox.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this event was successfully published, or when it
    /// was moved to the dead-letter state after exceeding <c>MaxRetries</c>.
    /// <see langword="null"/> means the event is still pending.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Gets or sets the number of failed publish attempts.</summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the error message from the last failed publish attempt, if any.
    /// </summary>
    public string? Error { get; set; }
}
