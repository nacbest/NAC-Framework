namespace Nac.Cqrs.Commands;

/// <summary>
/// Defines a handler for a command of type <typeparamref name="TCommand"/>
/// that returns a <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TCommand">The command type this handler processes.</typeparam>
/// <typeparam name="TResponse">The type of result returned after handling.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the given <paramref name="command"/> asynchronously.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response produced by handling the command.</returns>
    ValueTask<TResponse> HandleAsync(TCommand command, CancellationToken ct = default);
}
