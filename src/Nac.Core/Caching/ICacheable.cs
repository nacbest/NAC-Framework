namespace Nac.Core.Caching;

/// <summary>
/// Marker interface for queries that support caching.
/// The caching pipeline behavior checks the cache before invoking the handler.
/// </summary>
public interface ICacheable
{
    /// <summary>Cache key for this query instance.</summary>
    string CacheKey { get; }

    /// <summary>Optional cache expiry. Null means use the default expiry from configuration.</summary>
    TimeSpan? Expiry => null;
}
