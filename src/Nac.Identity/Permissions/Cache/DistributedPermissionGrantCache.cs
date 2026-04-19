using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Nac.Identity.Permissions.Cache;

/// <summary>
/// <see cref="IPermissionGrantCache"/> backed by <see cref="IDistributedCache"/>.
/// Serialises grant sets as JSON string arrays. Tracks keys in a concurrent set to
/// support pattern invalidation on memory caches (Redis hosts can override with SCAN).
/// </summary>
internal sealed class DistributedPermissionGrantCache(IDistributedCache cache) : IPermissionGrantCache
{
    private static readonly ConcurrentDictionary<string, byte> KnownKeys = new();

    public async Task<HashSet<string>> GetOrLoadAsync(string cacheKey,
                                                     Func<CancellationToken, Task<HashSet<string>>> factory,
                                                     TimeSpan ttl, CancellationToken ct = default)
    {
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            var arr = JsonSerializer.Deserialize<string[]>(cached) ?? [];
            return new HashSet<string>(arr, StringComparer.Ordinal);
        }

        var set = await factory(ct);
        var payload = JsonSerializer.Serialize(set);
        await cache.SetStringAsync(cacheKey, payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
        }, ct);
        KnownKeys.TryAdd(cacheKey, 0);
        return set;
    }

    public async Task InvalidateAsync(string cacheKey, CancellationToken ct = default)
    {
        await cache.RemoveAsync(cacheKey, ct);
        KnownKeys.TryRemove(cacheKey, out _);
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default)
    {
        // Only supports trailing wildcard. More complex globs are host-impl concerns.
        var trimmed = pattern.TrimEnd('*');
        var matching = KnownKeys.Keys.Where(k => k.StartsWith(trimmed, StringComparison.Ordinal)).ToList();

        foreach (var key in matching)
        {
            await cache.RemoveAsync(key, ct);
            KnownKeys.TryRemove(key, out _);
        }
    }
}
