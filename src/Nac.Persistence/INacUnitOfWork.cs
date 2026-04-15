using Nac.Core.Messaging;
using Nac.Core.Persistence;

namespace Nac.Persistence;

/// <summary>
/// Extends <see cref="IUnitOfWork"/> with the ability to collect domain events
/// from tracked aggregate roots after SaveChanges. Used internally by
/// <see cref="UnitOfWork.UnitOfWorkBehavior{TCommand,TResponse}"/>.
/// </summary>
public interface INacUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Collects all pending domain events from tracked entities and clears them.
    /// Call only after a successful <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    IReadOnlyList<INotification> CollectAndClearDomainEvents();
}
