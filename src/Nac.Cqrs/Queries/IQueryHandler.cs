namespace Nac.Cqrs.Queries;

/// <summary>
/// Defines a handler for a query of type <typeparamref name="TQuery"/>
/// that returns a <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TQuery">The query type this handler processes.</typeparam>
/// <typeparam name="TResponse">The type of data returned by the query handler.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the given <paramref name="query"/> asynchronously.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The data produced by the query.</returns>
    ValueTask<TResponse> HandleAsync(TQuery query, CancellationToken ct = default);
}
