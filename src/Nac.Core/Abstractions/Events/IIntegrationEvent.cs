namespace Nac.Core.Abstractions.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
