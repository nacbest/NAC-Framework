using Nac.Core.Abstractions.Events;

namespace Nac.EventBus.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default);
    Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default);
}
