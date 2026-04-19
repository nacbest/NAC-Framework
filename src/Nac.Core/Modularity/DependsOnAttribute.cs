namespace Nac.Core.Modularity;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DependsOnAttribute(params Type[] dependedModuleTypes) : Attribute
{
    public Type[] DependedModuleTypes { get; } = [.. dependedModuleTypes];
}
