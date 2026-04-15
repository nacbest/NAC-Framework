using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nac.Core.Caching;
using Nac.Mediator.Abstractions;
using Nac.Mediator.Core;

namespace Nac.Caching;

/// <summary>
/// Query pipeline behavior that caches results for queries implementing <see cref="ICacheable"/>.
/// Checks <see cref="IDistributedCache"/> before invoking the handler. On cache miss,
/// invokes the handler and stores the result.
/// </summary>
public sealed class CachingQueryBehavior<TQuery, TResponse>
    : IQueryBehavior<TQuery, TResponse>
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingQueryBehavior<TQuery, TResponse>> _logger;

    public CachingQueryBehavior(
        IDistributedCache cache,
        ILogger<CachingQueryBehavior<TQuery, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (query is not ICacheable cacheable)
            return await next(ct);

        // Try cache hit
        var cached = await _cache.GetStringAsync(cacheable.CacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheable.CacheKey);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        // Cache miss — invoke handler and store result
        var result = await next(ct);

        var expiry = cacheable.Expiry ?? DefaultExpiry;
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry,
        };

        await _cache.SetStringAsync(
            cacheable.CacheKey,
            JsonSerializer.Serialize(result),
            options,
            ct);

        _logger.LogDebug("Cached {CacheKey} for {Expiry}", cacheable.CacheKey, expiry);

        return result;
    }
}
