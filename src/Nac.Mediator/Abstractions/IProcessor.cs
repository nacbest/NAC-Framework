namespace Nac.Mediator.Abstractions;

/// <summary>
/// Pre-processor that runs after all behaviors have entered, just before the handler.
/// Use for enrichment, logging, or setup that doesn't need to wrap the handler.
/// </summary>
public interface IPreProcessor<in TRequest>
{
    Task ProcessAsync(TRequest request, CancellationToken ct);
}

/// <summary>
/// Post-processor that runs after the handler returns, before behaviors unwind.
/// Use for publishing notifications, cache invalidation, or audit logging.
/// </summary>
public interface IPostProcessor<in TRequest, in TResponse>
{
    Task ProcessAsync(TRequest request, TResponse response, CancellationToken ct);
}
