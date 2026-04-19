using Nac.Core.Modularity;
using Nac.Testing.Extensions;

namespace Nac.Testing;

/// <summary>
/// NAC Testing module — registers all fake implementations for unit testing.
/// </summary>
[DependsOn(typeof(NacCoreModule))]
public sealed class NacTestingModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddNacTesting();
    }
}
