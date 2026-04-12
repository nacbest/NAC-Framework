using Nac.Messaging;

namespace Nac.Mediator;

/// <summary>
/// Handler for queries. Exactly one handler per query type.
/// Query handlers must not change state.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
