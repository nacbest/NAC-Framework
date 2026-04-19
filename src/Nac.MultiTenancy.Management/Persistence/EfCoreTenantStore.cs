using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Management.Domain;

namespace Nac.MultiTenancy.Management.Persistence;

/// <summary>
/// EF Core-backed <see cref="ITenantStore"/> that resolves tenants from the
/// management registry. Active + non-deleted only — soft-deleted tenants never
/// resolve. Reads are cached with sliding TTL via <see cref="IMemoryCache"/>;
/// the cache is invalidated by <see cref="TenantCacheInvalidator"/> after every
/// mutation in <c>ITenantManagementService</c>.
/// </summary>
public sealed class EfCoreTenantStore : ITenantStore
{
    private readonly TenantManagementDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initialises a new instance of <see cref="EfCoreTenantStore"/>.
    /// </summary>
    public EfCoreTenantStore(TenantManagementDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<TenantInfo?> GetByIdAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var key = TenantCacheInvalidator.IdentifierKeyPrefix + tenantId;
        if (_cache.TryGetValue(key, out TenantInfo? cached))
            return cached;

        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Identifier == tenantId && t.IsActive)
            .FirstOrDefaultAsync(ct);

        var info = tenant is null ? null : ToInfo(tenant);
        // Cache misses too (negative cache) — short window prevents thundering-herd lookups.
        _cache.Set(key, info, new MemoryCacheEntryOptions { SlidingExpiration = SlidingTtl });
        return info;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantInfo>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(TenantCacheInvalidator.ListKey, out IReadOnlyList<TenantInfo>? cached) && cached is not null)
            return cached;

        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(ct);

        IReadOnlyList<TenantInfo> list = tenants.Select(ToInfo).ToList();
        _cache.Set(TenantCacheInvalidator.ListKey, list,
            new MemoryCacheEntryOptions { SlidingExpiration = SlidingTtl });
        return list;
    }

    private static TenantInfo ToInfo(Tenant t) => new()
    {
        Id = t.Identifier,
        Name = t.Name,
        // Ciphertext intentionally returned here; the resolver layer unprotects on demand.
        ConnectionString = t.EncryptedConnectionString,
        IsActive = t.IsActive,
        Properties = new Dictionary<string, string?>(t.Properties, StringComparer.OrdinalIgnoreCase),
    };
}
