using Nac.Core.Modularity;
using Nac.Persistence;

namespace Nac.MultiTenancy.Management;

/// <summary>
/// NAC Tenant Management module — opinionated tenant CRUD admin API on top of
/// <see cref="NacMultiTenancyModule"/>. Provides EF-backed registry, encrypted
/// per-tenant connection strings, REST endpoints, and outbox-emitted domain events.
/// Service registration is delegated to
/// <c>IServiceCollection.AddNacTenantManagement(...)</c> (see Phase 06).
/// </summary>
[DependsOn(typeof(NacCoreModule))]
[DependsOn(typeof(NacMultiTenancyModule))]
[DependsOn(typeof(NacPersistenceModule))]
public sealed class NacTenantManagementModule : NacModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Registration delegated to AddNacTenantManagement(...) extension method.
    }
}
