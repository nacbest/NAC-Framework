namespace Nac.Observability;

using Nac.Core.Modularity;

/// <summary>
/// NAC Observability module — structured logging enrichment and diagnostic name constants.
/// </summary>
[DependsOn(typeof(NacCoreModule))]
public sealed class NacObservabilityModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // No DI registrations needed — module serves as dependency declaration
        // and exposes NacActivitySources/NacMeters constants
    }
}
