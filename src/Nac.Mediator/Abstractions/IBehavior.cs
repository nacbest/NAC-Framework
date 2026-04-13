using Nac.Mediator.Core;

namespace Nac.Mediator.Abstractions;

/// <summary>
/// Pipeline behavior for commands. Wraps the handler — can execute logic before/after,
/// modify the result, short-circuit, or retry.
/// Behaviors are applied in registration order (first registered = outermost).
/// For void commands, <typeparamref name="TResponse"/> is <see cref="Unit"/>.
/// </summary>
/// <remarks>
/// To create a behavior that only applies to certain commands (e.g., ITransactional),
/// check at runtime: <c>if (command is not ITransactional) return await next(ct);</c>.
/// </remarks>
public interface ICommandBehavior<in TCommand, TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}

/// <summary>
/// Pipeline behavior for queries. Same pattern as command behaviors but applied
/// to the query pipeline. Caching behavior is a typical example.
/// </summary>
public interface IQueryBehavior<in TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
