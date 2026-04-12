using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Abstractions.Messaging;
using Nac.Persistence;
using Nac.Persistence.Outbox;

namespace Nac.Messaging.Internal;

/// <summary>
/// Resolves <see cref="IIntegrationEventHandler{TEvent}"/> implementations from DI
/// and invokes them. Checks the Inbox table for deduplication when a
/// <see cref="NacDbContext"/> is available in the scope.
/// <para>
/// Does NOT call SaveChangesAsync — the caller (outbox worker or in-memory worker)
/// is responsible for persisting inbox records along with its own state changes.
/// </para>
/// </summary>
internal sealed class IntegrationEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventDispatcher> _logger;

    public IntegrationEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<IntegrationEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches an integration event to all registered handlers.
    /// Adds an <see cref="InboxMessage"/> to the change tracker for deduplication
    /// but does NOT call SaveChangesAsync.
    /// </summary>
    /// <returns>True if the event was dispatched; false if it was a duplicate (inbox hit).</returns>
    public async Task<bool> DispatchAsync(IIntegrationEvent @event, CancellationToken ct)
    {
        var dbContext = _serviceProvider.GetService<NacDbContext>();

        // Inbox deduplication
        if (dbContext is not null)
        {
            var alreadyProcessed = await dbContext.InboxMessages
                .AnyAsync(m => m.EventId == @event.EventId, ct);

            if (alreadyProcessed)
            {
                _logger.LogDebug("Skipping duplicate event {EventId} ({EventType})",
                    @event.EventId, @event.EventType);
                return false;
            }
        }

        // Resolve and invoke handlers
        var eventType = @event.GetType();
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerInterfaceType);

        foreach (var handler in handlers)
        {
            var method = handlerInterfaceType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;
            await (Task)method.Invoke(handler, [@event, ct])!;
        }

        // Record in inbox (change tracker only — caller persists)
        dbContext?.InboxMessages.Add(new InboxMessage
        {
            EventId = @event.EventId,
            EventType = @event.EventType,
        });

        return true;
    }
}
