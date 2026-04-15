using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nac.WebApi.Modularity;

/// <summary>
/// Builder used in composition root to register NAC modules and framework features.
/// Extension methods from other NAC packages (Persistence, Auth, etc.) extend this type.
/// </summary>
public sealed class NacFrameworkBuilder
{
    private readonly List<INacModule> _modules = [];
    private readonly List<Assembly> _moduleAssemblies = [];

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }

    /// <summary>Registered modules in dependency order.</summary>
    internal IReadOnlyList<INacModule> Modules => _modules;

    /// <summary>Assemblies from registered modules, used for IEndpointMapper auto-discovery.</summary>
    internal IReadOnlyList<Assembly> ModuleAssemblies => _moduleAssemblies.AsReadOnly();

    internal NacFrameworkBuilder(IServiceCollection services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
    }

    /// <summary>Register a module. Modules are initialized in registration order.</summary>
    public NacFrameworkBuilder AddModule<TModule>() where TModule : INacModule, new()
    {
        var module = new TModule();

        if (_modules.Any(m => m.GetType() == typeof(TModule)))
            throw new InvalidOperationException($"Module '{module.Name}' is already registered.");

        _modules.Add(module);
        TrackAssembly(typeof(TModule).Assembly);
        return this;
    }

    /// <summary>Register a module instance.</summary>
    public NacFrameworkBuilder AddModule(INacModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_modules.Any(m => m.GetType() == module.GetType()))
            throw new InvalidOperationException($"Module '{module.Name}' is already registered.");

        _modules.Add(module);
        TrackAssembly(module.GetType().Assembly);
        return this;
    }

    private void TrackAssembly(Assembly assembly)
    {
        if (!_moduleAssemblies.Contains(assembly))
            _moduleAssemblies.Add(assembly);
    }
}
