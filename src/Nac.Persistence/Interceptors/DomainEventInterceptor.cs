using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Primitives;

namespace Nac.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that harvests domain events from all <see cref="AggregateRoot{TId}"/>
/// instances tracked by the context <em>after</em> a successful <c>SaveChangesAsync</c>, then
/// dispatches them via <see cref="IDomainEventDispatcher"/> if one is registered in the DI container.
/// If no dispatcher is registered the interceptor is a no-op — no exception is thrown.
/// </summary>
internal sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="DomainEventInterceptor"/>.
    /// </summary>
    /// <param name="serviceProvider">
    /// Used to optionally resolve <see cref="IDomainEventDispatcher"/> at dispatch time.
    /// </param>
    public DomainEventInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Collects all pending domain events from tracked aggregates, clears them, then
    /// forwards them to the optional <see cref="IDomainEventDispatcher"/>.
    /// </summary>
    private async Task DispatchDomainEventsAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        CancellationToken cancellationToken)
    {
        // Collect events from every tracked entity that exposes DomainEvents.
        var aggregates = context.ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Clear before dispatch to prevent re-processing on nested saves.
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        if (events.Count == 0)
            return;

        var dispatcher = _serviceProvider.GetService<IDomainEventDispatcher>();
        if (dispatcher is null)
            return;

        await dispatcher.DispatchAsync(events, cancellationToken);
    }
}
