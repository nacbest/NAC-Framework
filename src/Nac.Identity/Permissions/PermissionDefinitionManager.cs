using System.Collections.Frozen;
using Nac.Core.Abstractions.Permissions;

namespace Nac.Identity.Permissions;

/// <summary>
/// Singleton that builds and caches the full permission definition tree by
/// calling every registered <see cref="IPermissionDefinitionProvider"/> once
/// at construction time.
/// </summary>
public sealed class PermissionDefinitionManager
{
    private readonly IReadOnlyList<PermissionGroup> _groups;

    /// <summary>
    /// All permissions (including children at every depth) keyed by name.
    /// Built once; immutable after construction.
    /// </summary>
    private readonly FrozenDictionary<string, PermissionDefinition> _allPermissions;

    public PermissionDefinitionManager(IEnumerable<IPermissionDefinitionProvider> providers)
    {
        var context = new PermissionDefinitionContext();

        foreach (var provider in providers)
            provider.Define(context);

        _groups = context.Groups.Values.ToList().AsReadOnly();

        var flat = _groups.SelectMany(g => FlattenPermissions(g.Permissions)).ToList();
        var duplicates = flat.GroupBy(p => p.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Duplicate permission names detected: {string.Join(", ", duplicates)}");

        _allPermissions = flat.ToFrozenDictionary(p => p.Name, StringComparer.Ordinal);
    }

    /// <summary>Returns the definition for <paramref name="name"/>, or null if unknown.</summary>
    public PermissionDefinition? GetOrNull(string name) =>
        _allPermissions.TryGetValue(name, out var def) ? def : null;

    /// <summary>Returns all top-level permission groups.</summary>
    public IReadOnlyList<PermissionGroup> GetGroups() => _groups;

    /// <summary>Returns every registered permission definition (all depths).</summary>
    public IReadOnlyCollection<PermissionDefinition> GetAll() => _allPermissions.Values;

    // ── helpers ──────────────────────────────────────────────────────────────

    private static IEnumerable<PermissionDefinition> FlattenPermissions(
        IEnumerable<PermissionDefinition> permissions)
    {
        foreach (var permission in permissions)
        {
            yield return permission;

            foreach (var child in FlattenPermissions(permission.Children))
                yield return child;
        }
    }
}
