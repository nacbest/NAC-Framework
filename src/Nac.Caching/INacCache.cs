namespace Nac.Caching;

/// <summary>
/// Abstraction over the caching layer with tenant-aware key prefixing and tag-based invalidation.
/// </summary>
public interface INacCache
{
    /// <summary>
    /// Gets the cached value for <paramref name="key"/>, or creates and stores it using
    /// <paramref name="factory"/> when the entry is absent or expired.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The logical cache key (tenant prefix is applied automatically).</param>
    /// <param name="factory">Async delegate invoked on cache miss to produce the value.</param>
    /// <param name="options">Optional per-entry TTL and tag configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached or freshly created value.</returns>
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Unconditionally writes <paramref name="value"/> to the cache under <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The logical cache key (tenant prefix is applied automatically).</param>
    /// <param name="value">The value to store.</param>
    /// <param name="options">Optional per-entry TTL and tag configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Removes the cache entry identified by <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The logical cache key (tenant prefix is applied automatically).</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Removes all cache entries associated with <paramref name="tag"/>.
    /// The tag is automatically prefixed with the current tenant identifier when available.
    /// </summary>
    /// <param name="tag">The tag whose entries should be evicted.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default);
}
