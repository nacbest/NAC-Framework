using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nac.Core.Caching;
using Nac.Mediator.Abstractions;
using Nac.Mediator.Core;

namespace Nac.Caching;

/// <summary>
/// Command pipeline behavior that evicts cache entries after a successful command execution.
/// Only runs for commands implementing <see cref="ICacheInvalidator"/>.
/// Executes AFTER the handler so that stale data is only evicted on success.
/// </summary>
public sealed class CacheInvalidationBehavior<TCommand, TResponse>
    : ICommandBehavior<TCommand, TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheInvalidationBehavior<TCommand, TResponse>> _logger;

    public CacheInvalidationBehavior(
        IDistributedCache cache,
        ILogger<CacheInvalidationBehavior<TCommand, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var result = await next(ct);

        if (command is not ICacheInvalidator invalidator)
            return result;

        foreach (var key in invalidator.CacheKeysToInvalidate)
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogDebug("Invalidated cache key {CacheKey}", key);
        }

        return result;
    }
}
