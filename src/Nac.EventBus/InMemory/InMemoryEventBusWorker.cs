using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;

namespace Nac.EventBus.InMemory;

internal sealed class InMemoryEventBusWorker(
    Channel<IIntegrationEvent> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryEventBusWorker> logger) : BackgroundService
{
    private readonly ChannelReader<IIntegrationEvent> _reader = channel.Reader;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _reader.ReadAllAsync(stoppingToken))
        {
            await DispatchEventAsync(@event, stoppingToken);
        }

        // Drain remaining events after cancellation — catch per-event to avoid losing remaining events
        while (_reader.TryRead(out var remaining))
        {
            try
            {
                await DispatchEventAsync(remaining, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to drain event {EventType} during shutdown.",
                    remaining.GetType().Name);
            }
        }
    }

    private async Task DispatchEventAsync(IIntegrationEvent @event, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
            await dispatcher.DispatchAsync(@event, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Failed to dispatch event {EventType} ({EventId}).",
                @event.GetType().Name, @event.EventId);
        }
    }
}
