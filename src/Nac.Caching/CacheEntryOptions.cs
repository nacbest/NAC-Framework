namespace Nac.Caching;

/// <summary>
/// Per-entry configuration for cache operations, including TTL and tag associations.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>
    /// Gets the expiration duration for this cache entry.
    /// When <see langword="null"/>, the <see cref="NacCacheOptions.DefaultExpiration"/> is used.
    /// </summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>
    /// Gets the tags associated with this cache entry, used for bulk invalidation.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
