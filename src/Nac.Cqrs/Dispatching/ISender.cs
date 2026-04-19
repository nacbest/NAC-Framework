namespace Nac.Cqrs.Dispatching;

/// <summary>
/// Dispatches a request to its registered handler, executing any pipeline behaviors
/// in registration order before invoking the handler.
/// <para>
/// <b>Important:</b> This service must be resolved from a scoped DI container (e.g. per
/// HTTP request). Resolving from the root container will cause scoped dependencies
/// (handlers, behaviors, IUnitOfWork) to be resolved from the root scope, leading to
/// lifetime violations.
/// </para>
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request and returns the handler's response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The command or query to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response produced by the handler (and any pipeline behaviors).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the given request type.
    /// </exception>
    ValueTask<TResponse> SendAsync<TResponse>(
        IBaseRequest<TResponse> request,
        CancellationToken ct = default);
}
