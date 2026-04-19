using Nac.MultiTenancy.Abstractions;

namespace Nac.MultiTenancy.Context;

// Registered as Singleton — AsyncLocal handles per-async-context scoping (same pattern as HttpContext).
internal sealed class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<TenantInfo?> _current = new();

    public TenantInfo? Current => _current.Value;

    public void SetCurrentTenant(TenantInfo? tenant) => _current.Value = tenant;
}
