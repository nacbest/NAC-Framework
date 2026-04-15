using System.Reflection;

namespace Nac.WebApi.Modularity;

/// <summary>
/// Tracks assemblies from registered modules for endpoint auto-discovery.
/// Registered as singleton during AddNacFramework.
/// </summary>
public sealed class NacModuleAssemblyRegistry(IReadOnlyList<Assembly> assemblies)
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
}
