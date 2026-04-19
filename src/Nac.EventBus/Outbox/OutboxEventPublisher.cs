using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;
using Nac.Persistence.Outbox;

namespace Nac.EventBus.Outbox;

/// <summary>
/// Bridges OutboxWorker (string eventType + JSON payload) to the typed
/// EventBus IEventPublisher. Registered as IIntegrationEventPublisher in DI.
/// </summary>
internal sealed class OutboxEventPublisher(
    IEventPublisher eventPublisher,
    OutboxEventTypeRegistry typeRegistry,
    ILogger<OutboxEventPublisher> logger) : IIntegrationEventPublisher
{
    public async Task PublishAsync(string eventType, string payload, CancellationToken ct = default)
    {
        var resolvedType = typeRegistry.Resolve(eventType);
        if (resolvedType is null)
        {
            logger.LogError(
                "Unknown event type '{EventType}'. Not in allowlist. Skipping.",
                eventType);
            return;
        }

        IIntegrationEvent? typedEvent;
        try
        {
            var deserialized = JsonSerializer.Deserialize(payload, resolvedType);
            typedEvent = deserialized as IIntegrationEvent;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize outbox payload for event type '{EventType}'. Skipping.",
                eventType);
            return;
        }

        if (typedEvent is null)
        {
            logger.LogError(
                "Deserialized object for '{EventType}' is not IIntegrationEvent. Skipping.",
                eventType);
            return;
        }

        await eventPublisher.PublishAsync(typedEvent, ct);
    }
}
