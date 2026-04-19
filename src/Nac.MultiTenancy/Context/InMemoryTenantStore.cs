using System.Collections.Frozen;
using Nac.MultiTenancy.Abstractions;

namespace Nac.MultiTenancy.Context;

// In-memory implementation for dev/testing. Replace with EF-backed store in production.
public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly FrozenDictionary<string, TenantInfo> _tenants;

    public InMemoryTenantStore(IEnumerable<TenantInfo> tenants)
    {
        _tenants = tenants.ToFrozenDictionary(t => t.Id);
    }

    public Task<TenantInfo?> GetByIdAsync(string tenantId, CancellationToken ct = default) =>
        Task.FromResult(_tenants.GetValueOrDefault(tenantId));

    public Task<IReadOnlyList<TenantInfo>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TenantInfo>>(_tenants.Values.ToList());
}
