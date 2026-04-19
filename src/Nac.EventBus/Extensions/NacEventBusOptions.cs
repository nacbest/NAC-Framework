using System.Reflection;

namespace Nac.EventBus.Extensions;

public sealed class NacEventBusOptions
{
    internal List<Assembly> Assemblies { get; } = [];
    internal bool UseInMemory { get; private set; } = true;

    public NacEventBusOptions RegisterHandlersFromAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
        return this;
    }

    public NacEventBusOptions UseInMemoryTransport()
    {
        UseInMemory = true;
        return this;
    }
}
