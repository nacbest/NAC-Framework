using Nac.Abstractions.MultiTenancy;

namespace Nac.MultiTenancy;

/// <summary>
/// Simple in-memory <see cref="ITenantStore"/> backed by a dictionary.
/// Suitable for development, testing, and configuration-driven tenant lists.
/// For production, implement <see cref="ITenantStore"/> against a database.
/// </summary>
public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly Dictionary<string, TenantInfo> _tenants;

    public InMemoryTenantStore(IEnumerable<TenantInfo> tenants)
    {
        _tenants = tenants.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
    }

    public Task<TenantInfo?> GetByIdAsync(string tenantId, CancellationToken ct = default)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }
}
