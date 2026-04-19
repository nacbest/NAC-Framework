namespace Nac.Cqrs.Markers;

/// <summary>
/// Marker interface that opts a query into the caching pipeline behavior.
/// Implement this on any query whose result should be cached by <c>CachingBehavior</c>.
/// </summary>
/// <example>
/// <code>
/// public record GetUserByIdQuery(Guid Id) : IQuery&lt;Result&lt;UserDto&gt;&gt;, ICacheableQuery
/// {
///     public string CacheKey => $"users:{Id}";
///     public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
/// }
/// </code>
/// </example>
public interface ICacheableQuery
{
    /// <summary>
    /// Gets the unique cache key for this query's result.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the optional TTL for this cache entry.
    /// When <see langword="null"/>, the cache layer's default expiration is used.
    /// </summary>
    TimeSpan? CacheDuration => null;

    /// <summary>
    /// Gets the tags associated with this cache entry, used for bulk invalidation.
    /// </summary>
    IReadOnlyList<string> CacheTags => [];
}
