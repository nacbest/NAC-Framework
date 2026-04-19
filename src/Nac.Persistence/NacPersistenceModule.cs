using Nac.Core.Modularity;

namespace Nac.Persistence;

/// <summary>
/// NAC Persistence module descriptor. Service registration is delegated to
/// <see cref="Extensions.ServiceCollectionExtensions.AddNacPersistence{TContext}"/>
/// which must be called explicitly by the consumer (requires DbContext type).
/// </summary>
[DependsOn(typeof(NacCoreModule))]
public sealed class NacPersistenceModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Registration delegated to AddNacPersistence<TContext>() — needs TContext from consumer.
    }
}
