namespace Nac.Cqrs.Queries;

/// <summary>
/// Marker interface for a query that returns a <typeparamref name="TResponse"/>.
/// Queries are read-only operations with no side effects.
/// </summary>
/// <typeparam name="TResponse">The type of data returned by the query.</typeparam>
public interface IQuery<TResponse> : IBaseRequest<TResponse>;
