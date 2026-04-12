using Nac.Mediator.Abstractions;
using Nac.Mediator.Core;

namespace Nac.Persistence.UnitOfWork;

/// <summary>
/// Command pipeline behavior that persists tracked changes after the handler returns,
/// then dispatches collected domain events via the mediator.
/// Re-collects events in a loop because notification handlers may raise additional events.
/// </summary>
public sealed class UnitOfWorkBehavior<TCommand, TResponse>
    : ICommandBehavior<TCommand, TResponse>
{
    /// <summary>Safety guard against infinite domain event loops caused by handler bugs.</summary>
    private const int MaxDispatchRounds = 10;

    private readonly IEnumerable<INacUnitOfWork> _unitOfWorks;
    private readonly IMediator _mediator;

    public UnitOfWorkBehavior(IEnumerable<INacUnitOfWork> unitOfWorks, IMediator mediator)
    {
        _unitOfWorks = unitOfWorks;
        _mediator = mediator;
    }

    public async Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var result = await next(ct);

        // Flush tracked changes across all module contexts
        foreach (var uow in _unitOfWorks)
            await uow.SaveChangesAsync(ct);

        // Re-collect loop: notification handlers may modify entities and raise new events
        for (var round = 0; round < MaxDispatchRounds; round++)
        {
            var events = _unitOfWorks
                .SelectMany(uow => uow.CollectAndClearDomainEvents())
                .ToList();

            if (events.Count == 0)
                break;

            foreach (var domainEvent in events)
                await _mediator.PublishAsync(domainEvent, ct);

            // Notification handlers may have modified tracked entities — save again
            foreach (var uow in _unitOfWorks)
                await uow.SaveChangesAsync(ct);
        }

        return result;
    }
}
