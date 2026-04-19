namespace Nac.Core.Primitives;

/// <summary>
/// Base class for DDD aggregate roots. Inherits identity equality from <see cref="Entity{TId}"/>
/// and adds support for collecting and clearing domain events via <see cref="IHasDomainEvents"/>.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Appends a domain event to be dispatched after the next successful save.</summary>
    /// <param name="domainEvent">The event to enqueue.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
