using Nac.Abstractions.Messaging;

namespace Nac.Domain;

/// <summary>
/// Non-generic interface for entities that raise and hold domain events.
/// Used by the persistence layer to collect events after SaveChanges
/// without needing to know the entity's TId type parameter.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<INotification> DomainEvents { get; }
    void ClearDomainEvents();
}
