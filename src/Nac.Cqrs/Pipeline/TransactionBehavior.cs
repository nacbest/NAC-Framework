using Nac.Core.Abstractions;
using Nac.Cqrs.Markers;

namespace Nac.Cqrs.Pipeline;

/// <summary>
/// Pipeline behavior that automatically persists unit-of-work changes after a command
/// handler completes successfully.
/// <para>
/// Only activates when <typeparamref name="TRequest"/> implements <see cref="ITransactionalCommand"/>.
/// If the handler throws, the exception propagates and the unit of work is never flushed —
/// EF Core rolls back the transaction automatically.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The request type passing through this behavior.</typeparam>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
internal sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the behavior with the unit of work resolved from DI.
    /// </summary>
    /// <param name="unitOfWork">The unit of work scoped to the current request.</param>
    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        // Short-circuit for non-transactional requests.
        if (request is not ITransactionalCommand)
            return await next().ConfigureAwait(false);

        var response = await next().ConfigureAwait(false);

        // Flush pending changes only after the handler succeeds.
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return response;
    }
}
