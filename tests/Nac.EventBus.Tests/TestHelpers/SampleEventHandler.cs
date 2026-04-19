using Nac.EventBus.Abstractions;

namespace Nac.EventBus.Tests.TestHelpers;

public sealed class SampleEventHandler : IEventHandler<SampleIntegrationEvent>
{
    public List<SampleIntegrationEvent> Received { get; } = [];

    public Task HandleAsync(SampleIntegrationEvent @event, CancellationToken ct = default)
    {
        Received.Add(@event);
        return Task.CompletedTask;
    }
}
