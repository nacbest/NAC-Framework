namespace Nac.Core.Abstractions.Permissions;

public sealed class PermissionDefinition
{
    public string Name { get; }
    public string? DisplayName { get; }
    public bool IsEnabled { get; set; } = true;

    private readonly List<PermissionDefinition> _children = [];
    public IReadOnlyList<PermissionDefinition> Children => _children.AsReadOnly();

    internal PermissionDefinition(string name, string? displayName = null)
    {
        Name = name;
        DisplayName = displayName;
    }

    public PermissionDefinition AddChild(string name, string? displayName = null)
    {
        var child = new PermissionDefinition(name, displayName);
        _children.Add(child);
        return child;
    }
}
