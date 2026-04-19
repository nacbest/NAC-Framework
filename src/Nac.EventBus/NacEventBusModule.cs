using Nac.Core.Modularity;
using Nac.EventBus.Extensions;

namespace Nac.EventBus;

[DependsOn(typeof(NacCoreModule))]
public sealed class NacEventBusModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddNacEventBus(opts =>
            opts.RegisterHandlersFromAssembly(GetType().Assembly));
    }
}
