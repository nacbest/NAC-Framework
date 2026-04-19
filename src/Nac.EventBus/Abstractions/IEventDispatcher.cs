using Nac.Core.Abstractions.Events;

namespace Nac.EventBus.Abstractions;

public interface IEventDispatcher
{
    Task DispatchAsync(IIntegrationEvent @event, CancellationToken ct = default);
}
