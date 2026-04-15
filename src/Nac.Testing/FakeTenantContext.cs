using Nac.Core.MultiTenancy;

namespace Nac.Testing;

/// <summary>
/// Configurable <see cref="ITenantContext"/> for tests. Defaults to single-tenant mode.
/// Set <see cref="Current"/> to simulate multi-tenant scenarios.
/// </summary>
public sealed class FakeTenantContext : ITenantContext
{
    public TenantInfo? Current { get; set; }
    public bool IsMultiTenant => Current is not null;
}
