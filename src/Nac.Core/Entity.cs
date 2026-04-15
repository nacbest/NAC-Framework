using Nac.Core.Messaging;

namespace Nac.Core;

/// <summary>
/// Base class for all domain entities. Tracks domain events raised during the lifecycle
/// of the entity. Events are collected and dispatched by the UnitOfWork after SaveChanges.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IHasDomainEvents, IEquatable<Entity<TId>> where TId : notnull
{
    private readonly List<INotification> _domainEvents = [];

    /// <summary>The entity's unique identifier.</summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>Domain events raised by this entity, pending dispatch.</summary>
    public IReadOnlyList<INotification> DomainEvents => _domainEvents;

    /// <summary>Raises a domain event. The event is dispatched after UnitOfWork commit.</summary>
    protected void RaiseDomainEvent(INotification domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>Clears all pending domain events. Called by infrastructure after dispatch.</summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;

        // Transient entities (default ID) use reference equality
        if (EqualityComparer<TId>.Default.Equals(Id, default!))
            return ReferenceEquals(this, other);

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
        => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode()
    {
        // Transient entities use base hash to avoid all-same-hash buckets
        return EqualityComparer<TId>.Default.Equals(Id, default!)
            ? base.GetHashCode()
            : EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !Equals(left, right);
}
