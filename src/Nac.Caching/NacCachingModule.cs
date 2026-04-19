using Nac.Caching.Extensions;
using Nac.Core.Modularity;

namespace Nac.Caching;

/// <summary>
/// NAC Caching module — registers default <see cref="INacCache"/> implementation.
/// </summary>
[DependsOn(typeof(NacCoreModule))]
public sealed class NacCachingModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddNacCaching();
    }
}
