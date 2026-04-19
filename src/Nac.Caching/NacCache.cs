using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Nac.Core.Abstractions.Identity;

namespace Nac.Caching;

/// <summary>
/// <see cref="INacCache"/> implementation backed by <see cref="HybridCache"/>.
/// Automatically prefixes keys and tags with the current tenant identifier when
/// an <see cref="ICurrentUser"/> is available in the DI container.
/// </summary>
internal sealed class NacCache : INacCache
{
    private readonly HybridCache _hybridCache;
    private readonly NacCacheOptions _options;
    private readonly string? _tenantId;

    /// <summary>
    /// Initialises a new instance of <see cref="NacCache"/>.
    /// </summary>
    /// <param name="hybridCache">The underlying <see cref="HybridCache"/> instance.</param>
    /// <param name="serviceProvider">
    /// Service provider used to optionally resolve <see cref="ICurrentUser"/>.
    /// </param>
    /// <param name="options">Global caching options.</param>
    public NacCache(
        HybridCache hybridCache,
        IServiceProvider serviceProvider,
        IOptions<NacCacheOptions> options)
    {
        _hybridCache = hybridCache;
        _options = options.Value;

        var currentUser = serviceProvider.GetService(typeof(ICurrentUser)) as ICurrentUser;
        _tenantId = currentUser?.TenantId;
    }

    /// <inheritdoc />
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        var fullKey = CacheKey.Create(_tenantId, key);
        var entryOptions = BuildHybridOptions(options);
        var tags = BuildTags(options?.Tags);

        return _hybridCache.GetOrCreateAsync<T>(fullKey, factory, entryOptions, tags, ct);
    }

    /// <inheritdoc />
    public ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        var fullKey = CacheKey.Create(_tenantId, key);
        var entryOptions = BuildHybridOptions(options);
        var tags = BuildTags(options?.Tags);

        return _hybridCache.SetAsync<T>(fullKey, value, entryOptions, tags, ct);
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken ct = default)
    {
        var fullKey = CacheKey.Create(_tenantId, key);
        return _hybridCache.RemoveAsync(fullKey, ct);
    }

    /// <inheritdoc />
    public ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
        var fullTag = _tenantId is not null ? $"{_tenantId}:{tag}" : tag;
        return _hybridCache.RemoveByTagAsync(fullTag, ct);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private HybridCacheEntryOptions BuildHybridOptions(CacheEntryOptions? options)
    {
        var expiration = options?.Expiration ?? _options.DefaultExpiration;
        return new HybridCacheEntryOptions { Expiration = expiration };
    }

    private IReadOnlyCollection<string>? BuildTags(IReadOnlyList<string>? tags)
    {
        if (tags is not { Count: > 0 }) return null;
        if (_tenantId is null) return tags;
        return tags.Select(t => $"{_tenantId}:{t}").ToList();
    }
}
