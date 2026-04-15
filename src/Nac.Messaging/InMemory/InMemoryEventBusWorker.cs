using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nac.Messaging.Internal;

namespace Nac.Messaging.InMemory;

/// <summary>
/// Background worker that reads integration events from the <see cref="InMemoryEventBus"/>
/// channel and dispatches them to handlers in a new DI scope.
/// On shutdown, drains any remaining events in the channel before stopping.
/// </summary>
internal sealed class InMemoryEventBusWorker : BackgroundService
{
    private readonly InMemoryEventBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InMemoryEventBusWorker> _logger;

    public InMemoryEventBusWorker(
        InMemoryEventBus bus,
        IServiceScopeFactory scopeFactory,
        ILogger<InMemoryEventBusWorker> logger)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var @event in _bus.Reader.ReadAllAsync(ct))
            {
                await DispatchEventAsync(@event, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Graceful shutdown: drain remaining events from the channel
            _logger.LogInformation("InMemoryEventBus shutting down, draining remaining events");

            while (_bus.Reader.TryRead(out var @event))
            {
                await DispatchEventAsync(@event, CancellationToken.None);
            }
        }
    }

    private async Task DispatchEventAsync(
        Nac.Core.Messaging.IIntegrationEvent @event,
        CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IntegrationEventDispatcher>();
            await dispatcher.DispatchAsync(@event, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Failed to dispatch integration event {EventType} ({EventId})",
                @event.EventType, @event.EventId);
        }
    }
}
