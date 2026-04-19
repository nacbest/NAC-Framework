using Nac.Core.Modularity;
using Nac.Persistence;

namespace Nac.MultiTenancy;

/// <summary>
/// NAC Multi-Tenancy module — tenant resolution and per-tenant data isolation.
/// </summary>
[DependsOn(typeof(NacCoreModule))]
[DependsOn(typeof(NacPersistenceModule))]
public sealed class NacMultiTenancyModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Registration delegated to IServiceCollection extension method in Phase 2.
    }
}
