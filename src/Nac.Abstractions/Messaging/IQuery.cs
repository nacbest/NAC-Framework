namespace Nac.Messaging;

/// <summary>
/// Marker interface for queries that return a result of type <typeparamref name="TResult"/>.
/// Queries are read-only operations — they must not change state.
/// Query pipeline is lighter than command pipeline (no transaction, no UoW).
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query handler.</typeparam>
public interface IQuery<out TResult>;
