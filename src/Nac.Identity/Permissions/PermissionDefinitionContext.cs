using Nac.Core.Abstractions.Permissions;

namespace Nac.Identity.Permissions;

/// <summary>
/// Mutable context passed to <see cref="IPermissionDefinitionProvider"/> implementations
/// during the permission definition phase. Groups are keyed by name.
/// </summary>
internal sealed class PermissionDefinitionContext : IPermissionDefinitionContext
{
    private readonly Dictionary<string, PermissionGroup> _groups = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public PermissionGroup AddGroup(string name, string? displayName = null)
    {
        if (_groups.ContainsKey(name))
            throw new InvalidOperationException(
                $"A permission group named '{name}' has already been registered.");

        var group = new PermissionGroup(name, displayName);
        _groups[name] = group;
        return group;
    }

    /// <inheritdoc/>
    public PermissionGroup? GetGroupOrNull(string name) =>
        _groups.TryGetValue(name, out var group) ? group : null;

    /// <summary>Returns all registered groups.</summary>
    public IReadOnlyDictionary<string, PermissionGroup> Groups => _groups;
}
