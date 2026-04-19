using Nac.Core.Abstractions.Events;

namespace Nac.EventBus.Abstractions;

public interface IEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
