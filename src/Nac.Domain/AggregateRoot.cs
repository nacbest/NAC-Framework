namespace Nac.Domain;

/// <summary>
/// Base class for aggregate roots — entities that serve as the transactional boundary.
/// Only aggregate roots should be persisted via repositories.
/// Child entities are managed through the aggregate root.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// Automatically managed by the persistence layer (EF Core RowVersion).
    /// </summary>
    public uint Version { get; protected set; }
}
