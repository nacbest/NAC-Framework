namespace Nac.Core.Modularity;

/// <summary>
/// Root module for the NAC Framework. All other modules depend on this.
/// Contains no service registrations — serves as the base of the dependency graph.
/// </summary>
public sealed class NacCoreModule : NacModule;
