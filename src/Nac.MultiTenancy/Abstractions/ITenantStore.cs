namespace Nac.MultiTenancy.Abstractions;

public interface ITenantStore
{
    Task<TenantInfo?> GetByIdAsync(string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantInfo>> GetAllAsync(CancellationToken ct = default);
}
