using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions;
using Nac.Core.Abstractions.Events;
using Nac.Core.Abstractions.Identity;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="OutboxInterceptor"/>.
    /// </summary>
    /// <param name="serviceProvider">
    /// Used to optionally resolve <see cref="ICurrentUser"/>; no exception is thrown if
    /// the service is not registered (e.g. background jobs, seeding).
    /// </param>
    /// <param name="dateTimeProvider">Provides the current UTC time.</param>
    public OutboxInterceptor(IServiceProvider serviceProvider, IDateTimeProvider dateTimeProvider)
    {
        _serviceProvider = serviceProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            // Resolve ICurrentUser from the current DI scope (request-scoped).
            // GetService returns null gracefully when no authenticated user exists
            // (background jobs, seeding, design-time) — envelope fields remain null.
            var currentUser = _serviceProvider.GetService<ICurrentUser>();
            WriteOutboxEvents(eventData.Context, _dateTimeProvider, currentUser);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Harvests integration events from all tracked aggregates and adds corresponding
    /// <see cref="OutboxEvent"/> rows to the context (not yet saved).
    /// Stamps <see cref="OutboxEvent.TenantId"/>, <see cref="OutboxEvent.ActorUserId"/>,
    /// and <see cref="OutboxEvent.ImpersonatorUserId"/> from <paramref name="currentUser"/>
    /// synchronously — before the request scope is disposed.
    /// </summary>
    private static void WriteOutboxEvents(
        Microsoft.EntityFrameworkCore.DbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser? currentUser)
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
                TenantId = currentUser?.TenantId,
                ActorUserId = currentUser?.Id,
                ImpersonatorUserId = currentUser?.ImpersonatorId,
            };
            outboxSet.Add(outboxEvent);
        }
    }
}
