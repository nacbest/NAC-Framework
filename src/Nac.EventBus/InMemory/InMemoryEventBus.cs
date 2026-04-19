using System.Threading.Channels;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;

namespace Nac.EventBus.InMemory;

internal sealed class InMemoryEventBus(Channel<IIntegrationEvent> channel) : IEventPublisher
{
    private readonly ChannelWriter<IIntegrationEvent> _writer = channel.Writer;

    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        await _writer.WriteAsync(@event, ct);
    }

    public async Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
            await _writer.WriteAsync(@event, ct);
    }
}
