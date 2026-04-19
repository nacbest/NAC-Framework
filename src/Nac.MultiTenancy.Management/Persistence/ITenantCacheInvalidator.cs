namespace Nac.MultiTenancy.Management.Persistence;

/// <summary>
/// Invalidates cached <c>TenantInfo</c> entries after the registry mutates a
/// tenant. Implementations are typically wrappers around <c>IMemoryCache</c>.
/// </summary>
public interface ITenantCacheInvalidator
{
    /// <summary>
    /// Removes any cache entries keyed by the given tenant identifier.
    /// Pass the public <c>Identifier</c> string used by <c>ITenantStore</c>.
    /// </summary>
    void Invalidate(string identifier);

    /// <summary>Drops the cached "all tenants" list, if any.</summary>
    void InvalidateList();
}
