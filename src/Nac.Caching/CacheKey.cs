namespace Nac.Caching;

/// <summary>
/// Utility for building consistent, tenant-aware cache keys.
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// Creates a cache key by joining <paramref name="segments"/> with <c>:</c> as separator.
    /// When <paramref name="tenantId"/> is provided it is prepended as the first segment,
    /// ensuring tenant isolation across a shared cache backend.
    /// </summary>
    /// <param name="tenantId">
    /// The tenant identifier to prefix the key, or <see langword="null"/> for no prefix.
    /// </param>
    /// <param name="segments">One or more key segments that identify the cached resource.</param>
    /// <returns>A colon-delimited cache key string.</returns>
    public static string Create(string? tenantId, params string[] segments)
    {
        var key = string.Join(":", segments);
        return tenantId is not null ? $"{tenantId}:{key}" : key;
    }
}
