using Nac.Core.Primitives;

namespace Nac.Persistence.Interceptors;

/// <summary>
/// Optional dispatcher that receives domain events collected by <see cref="DomainEventInterceptor"/>
/// after a successful <c>SaveChangesAsync</c>.
/// Register an implementation (e.g. a MediatR bridge) in the DI container; if none is registered
/// the interceptor silently skips dispatch.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches the supplied domain events to their registered handlers.
    /// </summary>
    /// <param name="events">The events harvested from all aggregates in the current unit of work.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default);
}
