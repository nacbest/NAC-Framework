using Nac.Caching;
using Nac.Cqrs.Markers;

namespace Nac.Cqrs.Pipeline;

/// <summary>
/// Pipeline behavior that caches query results using <see cref="INacCache"/>.
/// <para>
/// Only activates when <typeparamref name="TRequest"/> implements <see cref="ICacheableQuery"/>.
/// Non-cacheable requests pass through immediately with zero overhead.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The request type passing through this behavior.</typeparam>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
internal sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest<TResponse>
{
    private readonly INacCache _cache;

    /// <summary>
    /// Initializes the behavior with the NAC cache abstraction.
    /// </summary>
    /// <param name="cache">The cache implementation resolved from DI.</param>
    public CachingBehavior(INacCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc/>
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        // Short-circuit for non-cacheable requests.
        if (request is not ICacheableQuery cacheable)
            return await next().ConfigureAwait(false);

        var options = BuildCacheEntryOptions(cacheable);

        return await _cache.GetOrCreateAsync<TResponse>(
            cacheable.CacheKey,
            async innerCt => await next().ConfigureAwait(false),
            options,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Constructs a <see cref="CacheEntryOptions"/> from the cacheable query's configuration.
    /// Returns <see langword="null"/> when the query specifies no custom TTL or tags,
    /// allowing the cache layer to apply its own defaults.
    /// </summary>
    private static CacheEntryOptions? BuildCacheEntryOptions(ICacheableQuery cacheable)
    {
        var hasDuration = cacheable.CacheDuration.HasValue;
        var hasTags = cacheable.CacheTags.Count > 0;

        if (!hasDuration && !hasTags)
            return null;

        return new CacheEntryOptions
        {
            Expiration = cacheable.CacheDuration,
            Tags = cacheable.CacheTags,
        };
    }
}
