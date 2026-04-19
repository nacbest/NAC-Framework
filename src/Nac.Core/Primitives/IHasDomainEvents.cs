namespace Nac.Core.Primitives;

/// <summary>
/// Marks an aggregate as a source of domain events.
/// Implemented by <see cref="AggregateRoot{TId}"/>; can also be implemented by
/// non-generic aggregates when needed.
/// Infrastructure interceptors (e.g. EF Core post-save hooks) use this interface
/// to harvest and dispatch events without coupling to the generic <c>AggregateRoot&lt;TId&gt;</c>.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Gets the domain events raised since the last <see cref="ClearDomainEvents"/> call.</summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>Removes all domain events from the aggregate's internal list.</summary>
    void ClearDomainEvents();
}
