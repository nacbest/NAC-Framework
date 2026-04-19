namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Fluent builder for a single role template definition. Accumulated permission
/// names are deduplicated (preserving insertion order) to avoid duplicate grant
/// rows in the seeder.
/// </summary>
public sealed class RoleTemplateBuilder
{
    private readonly string _key;
    private readonly string _name;
    private readonly string? _description;

    // LinkedHashSet equivalent: insertion-ordered dedup.
    private readonly LinkedList<string> _permissionOrder = new();
    private readonly HashSet<string> _permissionSet = new(StringComparer.Ordinal);

    internal RoleTemplateBuilder(string key, string name, string? description)
    {
        _key = key;
        _name = name;
        _description = description;
    }

    /// <summary>
    /// Appends one or more permission names to the template. Duplicates are silently
    /// ignored (idempotent within a single builder chain).
    /// </summary>
    public RoleTemplateBuilder Grants(params string[] permissionNames)
    {
        foreach (var perm in permissionNames)
        {
            if (_permissionSet.Add(perm))
                _permissionOrder.AddLast(perm);
        }
        return this;
    }

    /// <summary>Builds the immutable <see cref="RoleTemplateDefinition"/>.</summary>
    internal RoleTemplateDefinition Build() =>
        new(_key, _name, _description, _permissionOrder.ToList().AsReadOnly());
}
