using System.Text.Json;
using Nac.Core.Messaging;
using Nac.Persistence;
using Nac.Persistence.Outbox;

namespace Nac.Messaging.Outbox;

/// <summary>
/// <see cref="IEventBus"/> implementation that writes integration events to the
/// <see cref="OutboxMessage"/> table in the same DbContext transaction as business data.
/// A background outbox worker later picks them up and dispatches to handlers.
/// <para>
/// Generic over <typeparamref name="TContext"/> so each module's context gets its own
/// outbox bus. The DI scope ensures the bus shares the same context instance as the
/// module's repositories.
/// </para>
/// </summary>
public sealed class OutboxEventBus<TContext> : IEventBus
    where TContext : NacDbContext
{
    private readonly TContext _context;

    public OutboxEventBus(TContext context) => _context = context;

    /// <summary>
    /// Serializes the event and adds it to <see cref="NacDbContext.OutboxMessages"/>.
    /// Does NOT call SaveChanges — the UnitOfWork behavior handles that.
    /// </summary>
    public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Id = @event.EventId,
            EventType = @event.EventType,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            OccurredAt = @event.OccurredAt,
        });
        return Task.CompletedTask;
    }
}
