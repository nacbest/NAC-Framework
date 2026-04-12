using System.Threading.Channels;
using Nac.Abstractions.Messaging;

namespace Nac.Messaging.InMemory;

/// <summary>
/// In-process <see cref="IEventBus"/> for development and single-process deployments.
/// Events are queued in a <see cref="Channel{T}"/> and dispatched asynchronously by
/// <see cref="InMemoryEventBusWorker"/>. Registered as a singleton so all publishers
/// share the same channel.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<IIntegrationEvent> _channel =
        Channel.CreateUnbounded<IIntegrationEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    internal ChannelReader<IIntegrationEvent> Reader => _channel.Reader;

    /// <summary>
    /// Queues the event for asynchronous dispatch. Returns immediately —
    /// the caller does not wait for handlers to complete.
    /// </summary>
    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(@event, ct);
    }
}
