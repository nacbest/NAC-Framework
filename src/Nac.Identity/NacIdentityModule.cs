using Nac.Core.Modularity;
using Nac.Persistence;

namespace Nac.Identity;

/// <summary>
/// NAC Framework module descriptor for the Identity package.
/// Service registration is delegated to the
/// <see cref="Extensions.ServiceCollectionExtensions.AddNacIdentity{TContext}"/> extension method,
/// which must be called explicitly by the host application during startup.
/// </summary>
[DependsOn(typeof(NacCoreModule))]
[DependsOn(typeof(NacPersistenceModule))]
public sealed class NacIdentityModule : NacModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Registration delegated to AddNacIdentity<TContext>() extension method.
    }
}
