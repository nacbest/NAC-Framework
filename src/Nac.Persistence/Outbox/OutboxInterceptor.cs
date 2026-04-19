using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nac.Core.Abstractions;
using Nac.Core.Abstractions.Events;
using Nac.Core.Primitives;

namespace Nac.Persistence.Outbox;

/// <summary>
/// EF Core save-changes interceptor that serialises any domain event that also implements
/// <see cref="IIntegrationEvent"/> into an <see cref="OutboxEvent"/> row, written in the
/// same database transaction as the business entity changes.
/// This guarantees at-least-once delivery: if the process crashes after commit the
/// <see cref="OutboxWorker"/> will retry from the persisted rows.
/// </summary>
internal sealed class OutboxInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="OutboxInterceptor"/>.
    /// </summary>
    /// <param name="dateTimeProvider">Provides the current UTC time.</param>
    public OutboxInterceptor(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            WriteOutboxEvents(eventData.Context, _dateTimeProvider);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Harvests integration events from all tracked aggregates and adds corresponding
    /// <see cref="OutboxEvent"/> rows to the context (not yet saved).
    /// </summary>
    private static void WriteOutboxEvents(Microsoft.EntityFrameworkCore.DbContext context, IDateTimeProvider dateTimeProvider)
    {
        var now = dateTimeProvider.UtcNow;

        var integrationEvents = context.ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .SelectMany(a => a.DomainEvents)
            .OfType<IIntegrationEvent>()
            .ToList();

        if (integrationEvents.Count == 0)
            return;

        var outboxSet = context.Set<OutboxEvent>();

        foreach (var integrationEvent in integrationEvents)
        {
            var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
            var outboxEvent = new OutboxEvent
            {
                EventType = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().FullName!,
                Payload = payload,
                CreatedAt = now,
            };
            outboxSet.Add(outboxEvent);
        }
    }
}
