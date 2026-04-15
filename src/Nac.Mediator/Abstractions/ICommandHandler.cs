using Nac.Core.Messaging;

namespace Nac.Mediator.Abstractions;

/// <summary>
/// Handler for void commands (ICommand). Exactly one handler per command type.
/// Handlers should not call SaveChanges — the UnitOfWork behavior does that.
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}

/// <summary>
/// Handler for commands that return a result (ICommand&lt;TResult&gt;).
/// Exactly one handler per command type.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}
