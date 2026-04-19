namespace Nac.Identity.Permissions.Cache;

/// <summary>
/// Cache surface for permission grant sets keyed by <see cref="PermissionCacheKeys"/>.
/// Wraps <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>; the
/// implementation is the freshness source of truth — invalidation on mutation is
/// mandatory (TTL is a safety net only).
/// </summary>
public interface IPermissionGrantCache
{
    /// <summary>Fetches from cache or loads via <paramref name="factory"/> and caches for <paramref name="ttl"/>.</summary>
    Task<HashSet<string>> GetOrLoadAsync(string cacheKey,
                                         Func<CancellationToken, Task<HashSet<string>>> factory,
                                         TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Invalidates the exact cache key.</summary>
    Task InvalidateAsync(string cacheKey, CancellationToken ct = default);

    /// <summary>
    /// Invalidates keys matching the given pattern. Memory impl uses a tracked key set;
    /// Redis impl uses SCAN. Pattern accepts trailing <c>*</c>.
    /// </summary>
    Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default);
}
