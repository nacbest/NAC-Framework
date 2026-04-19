using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nac.Core.Modularity;

/// <summary>
/// Orchestrates the module lifecycle: discovers modules, executes configuration hooks,
/// and stores the sorted module list for later use by the hosted service.
/// </summary>
public sealed class NacApplicationFactory
{
    private NacApplicationFactory(IReadOnlyList<NacModule> modules)
    {
        Modules = modules;
    }

    /// <summary>Sorted module list (dependencies before dependents).</summary>
    public IReadOnlyList<NacModule> Modules { get; }

    /// <summary>
    /// Creates the factory, discovers modules from <paramref name="startupModuleType"/>,
    /// and executes Pre/Config/Post ConfigureServices on all modules.
    /// </summary>
    public static NacApplicationFactory Create(
        Type startupModuleType,
        IServiceCollection services,
        IConfiguration configuration)
    {
        var modules = NacModuleLoader.LoadModules(startupModuleType);
        var context = new ServiceConfigurationContext(services, configuration);

        foreach (var module in modules)
            module.PreConfigureServices(context);

        foreach (var module in modules)
            module.ConfigureServices(context);

        foreach (var module in modules)
            module.PostConfigureServices(context);

        return new NacApplicationFactory(modules);
    }
}
