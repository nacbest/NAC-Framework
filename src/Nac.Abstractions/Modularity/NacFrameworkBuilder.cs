using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nac.Modularity;

/// <summary>
/// Builder used in composition root to register NAC modules and framework features.
/// Extension methods from other NAC packages (Persistence, Auth, etc.) extend this type.
/// </summary>
public sealed class NacFrameworkBuilder
{
    private readonly List<INacModule> _modules = [];

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }

    /// <summary>Registered modules in dependency order.</summary>
    internal IReadOnlyList<INacModule> Modules => _modules;

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
        return this;
    }

    /// <summary>Register a module instance.</summary>
    public NacFrameworkBuilder AddModule(INacModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_modules.Any(m => m.GetType() == module.GetType()))
            throw new InvalidOperationException($"Module '{module.Name}' is already registered.");

        _modules.Add(module);
        return this;
    }
}
