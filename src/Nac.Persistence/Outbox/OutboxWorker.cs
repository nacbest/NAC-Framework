using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions;
using Nac.Persistence.Context;

namespace Nac.Persistence.Outbox;

/// <summary>
/// Background service that polls the outbox table every 5 seconds, publishes pending
/// <see cref="OutboxEvent"/> rows via <see cref="IIntegrationEventPublisher"/>, and marks
/// them as processed.
/// Dead-letter threshold: events that fail <c>5</c> consecutive times are marked as processed
/// with the last error recorded so they do not block subsequent rows.
/// </summary>
internal sealed class OutboxWorker : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxWorker> _logger;
    private bool _publisherWarningLogged;

    /// <summary>
    /// Initialises a new instance of <see cref="OutboxWorker"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a DI scope per polling cycle.</param>
    /// <param name="logger">Logger for operational diagnostics.</param>
    public OutboxWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error in OutboxWorker polling cycle.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Creates a scoped DI container, fetches one batch of unprocessed rows, attempts to
    /// publish each one, and persists the updated state in a single <c>SaveChangesAsync</c>.
    /// </summary>
    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<NacDbContext>();
        var publisher = scope.ServiceProvider.GetService<IIntegrationEventPublisher>();

        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        if (publisher is null && !_publisherWarningLogged)
        {
            _logger.LogWarning(
                "No {Publisher} registered. Outbox events will be marked processed without publishing. " +
                "Register an IIntegrationEventPublisher implementation to enable real publishing.",
                nameof(IIntegrationEventPublisher));
            _publisherWarningLogged = true;
        }

        var pending = await context.Set<OutboxEvent>()
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        foreach (var outboxEvent in pending)
        {
            try
            {
                if (publisher is not null)
                    await publisher.PublishAsync(outboxEvent.EventType, outboxEvent.Payload, ct);

                outboxEvent.ProcessedAt = dateTimeProvider.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                outboxEvent.RetryCount++;
                outboxEvent.Error = ex.Message;

                _logger.LogWarning(ex,
                    "Failed to publish outbox event {EventId} (attempt {Attempt}).",
                    outboxEvent.Id, outboxEvent.RetryCount);

                if (outboxEvent.RetryCount >= MaxRetries)
                {
                    _logger.LogError(
                        "Outbox event {EventId} exceeded max retries ({MaxRetries}). Moving to dead-letter.",
                        outboxEvent.Id, MaxRetries);
                    outboxEvent.ProcessedAt = dateTimeProvider.UtcNow;
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
