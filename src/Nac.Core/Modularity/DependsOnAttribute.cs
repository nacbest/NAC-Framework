namespace Nac.Core.Modularity;

/// <summary>
/// Declares compile-time module dependencies. Applied to INacModule implementations.
/// Framework validates all declared dependencies are registered at startup.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DependsOnAttribute(params Type[] moduleTypes) : Attribute
{
    public Type[] ModuleTypes { get; } = moduleTypes;
}
