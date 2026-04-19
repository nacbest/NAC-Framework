using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;

namespace Nac.Testing.Fakes;

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<IIntegrationEvent> PublishedEvents { get; } = [];

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        PublishedEvents.Add(@event);
        return Task.CompletedTask;
    }

    public Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default)
    {
        PublishedEvents.AddRange(events);
        return Task.CompletedTask;
    }
}
