using Nac.EventBus.Abstractions;

namespace Nac.EventBus.Tests.TestHelpers;

public sealed class ThrowingEventHandler : IEventHandler<SampleIntegrationEvent>
{
    public Task HandleAsync(SampleIntegrationEvent @event, CancellationToken ct = default) =>
        throw new InvalidOperationException("Handler failed");
}
