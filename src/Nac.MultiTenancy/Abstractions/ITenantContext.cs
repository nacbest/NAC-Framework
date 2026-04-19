namespace Nac.MultiTenancy.Abstractions;

public interface ITenantContext
{
    TenantInfo? Current { get; }
    string? TenantId => Current?.Id;
    void SetCurrentTenant(TenantInfo? tenant);
}
