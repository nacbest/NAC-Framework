using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nac.Messaging.Internal;
using Nac.Persistence;
using Nac.Persistence.Outbox;

namespace Nac.Messaging.Outbox;

/// <summary>
/// Background worker that polls a module's <see cref="OutboxMessage"/> table
/// and dispatches unprocessed events to their handlers.
/// One worker is registered per module DbContext that uses outbox messaging.
/// </summary>
internal sealed class OutboxWorker<TContext> : BackgroundService
    where TContext : NacDbContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;
    private const int MaxRetries = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EventTypeRegistry _registry;
    private readonly ILogger<OutboxWorker<TContext>> _logger;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        EventTypeRegistry registry,
        ILogger<OutboxWorker<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(ct);

                if (processed == 0)
                    await Task.Delay(PollInterval, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox worker error for {Context}", typeof(TContext).Name);
                await Task.Delay(PollInterval, ct);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IntegrationEventDispatcher>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            // Dead-letter: poison messages that exceeded max retries
            if (message.RetryCount >= MaxRetries)
            {
                _logger.LogWarning(
                    "Outbox message {Id} exceeded {MaxRetries} retries, marking as dead-lettered",
                    message.Id, MaxRetries);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.Error = $"Dead-lettered after {MaxRetries} retries: {message.Error}";
                continue;
            }

            try
            {
                var eventType = _registry.Resolve(message.EventType);
                if (eventType is null)
                {
                    _logger.LogWarning("Unknown event type {EventType} in outbox message {Id}",
                        message.EventType, message.Id);
                    message.Error = $"Unknown event type: {message.EventType}";
                    message.RetryCount++;
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType);
                if (@event is not Nac.Core.Messaging.IIntegrationEvent integrationEvent)
                {
                    message.Error = "Deserialization returned non-IIntegrationEvent";
                    message.RetryCount++;
                    continue;
                }

                var dispatched = await dispatcher.DispatchAsync(integrationEvent, ct);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.Error = dispatched ? null : "Skipped (duplicate via inbox)";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                message.Error = ex.Message;
                message.RetryCount++;
            }
        }

        // Single SaveChanges: persists outbox status + inbox records atomically
        if (messages.Count > 0)
            await context.SaveChangesAsync(ct);

        return messages.Count;
    }
}
