using Nac.Core.Modularity;

namespace Nac.Cqrs;

/// <summary>
/// NAC CQRS module descriptor. Service registration is delegated to
/// <see cref="Extensions.ServiceCollectionExtensions.AddNacCqrs"/> which must be called
/// explicitly by the consumer (requires handler assembly reference).
/// </summary>
[DependsOn(typeof(NacCoreModule))]
public sealed class NacCqrsModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Registration delegated to AddNacCqrs() — needs assembly from consumer.
    }
}
