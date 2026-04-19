namespace Nac.Cqrs.Pipeline;

/// <summary>
/// Delegate representing the next step in the request-handling pipeline.
/// Invoke this to pass control to the next behavior or the final handler.
/// </summary>
/// <typeparam name="TResponse">The response type of the current pipeline step.</typeparam>
/// <returns>A <see cref="ValueTask{TResponse}"/> from the next step.</returns>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Pipeline behavior that wraps handler execution, enabling cross-cutting concerns
/// such as logging, validation, caching, or transaction management.
/// </summary>
/// <typeparam name="TRequest">The request type passing through this behavior.</typeparam>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IBaseRequest<TResponse>
{
    /// <summary>
    /// Executes the behavior logic, then calls <paramref name="next"/> to continue
    /// the pipeline.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">Delegate to invoke the next step in the pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response from the pipeline.</returns>
    ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default);
}
