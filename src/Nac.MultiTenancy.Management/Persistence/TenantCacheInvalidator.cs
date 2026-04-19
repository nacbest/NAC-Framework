using Microsoft.Extensions.Caching.Memory;

namespace Nac.MultiTenancy.Management.Persistence;

/// <summary>
/// Default <see cref="ITenantCacheInvalidator"/> backed by
/// <see cref="IMemoryCache"/>. Cache keys mirror those used by
/// <see cref="EfCoreTenantStore"/>.
/// </summary>
internal sealed class TenantCacheInvalidator : ITenantCacheInvalidator
{
    private readonly IMemoryCache _cache;

    /// <summary>Cache-key prefix for single-tenant lookups by identifier.</summary>
    public const string IdentifierKeyPrefix = "nac.mt.tenant.identifier:";

    /// <summary>Cache-key for the "all tenants" projection.</summary>
    public const string ListKey = "nac.mt.tenants.all";

    public TenantCacheInvalidator(IMemoryCache cache) => _cache = cache;

    /// <inheritdoc />
    public void Invalidate(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        _cache.Remove(IdentifierKeyPrefix + identifier);
        _cache.Remove(ListKey);
    }

    /// <inheritdoc />
    public void InvalidateList() => _cache.Remove(ListKey);
}
