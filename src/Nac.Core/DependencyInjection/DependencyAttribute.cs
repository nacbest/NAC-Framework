using Microsoft.Extensions.DependencyInjection;

namespace Nac.Core.DependencyInjection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class DependencyAttribute : Attribute
{
    /// <summary>
    /// If true, replaces existing service registration for the same interface.
    /// </summary>
    public bool ReplaceServices { get; set; }

    /// <summary>
    /// Override the lifetime detected by marker interface.
    /// </summary>
    public ServiceLifetime? Lifetime { get; set; }
}
