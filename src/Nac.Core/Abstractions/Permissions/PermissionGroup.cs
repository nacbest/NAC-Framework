namespace Nac.Core.Abstractions.Permissions;

public sealed class PermissionGroup
{
    public string Name { get; }
    public string? DisplayName { get; }

    private readonly List<PermissionDefinition> _permissions = [];
    public IReadOnlyList<PermissionDefinition> Permissions => _permissions.AsReadOnly();

    internal PermissionGroup(string name, string? displayName = null)
    {
        Name = name;
        DisplayName = displayName;
    }

    public PermissionDefinition AddPermission(string name, string? displayName = null)
    {
        var permission = new PermissionDefinition(name, displayName);
        _permissions.Add(permission);
        return permission;
    }
}
