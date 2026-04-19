namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Mutable implementation of <see cref="IRoleTemplateContext"/> used during the startup
/// registration phase. <see cref="Build"/> is called once by
/// <see cref="RoleTemplateDefinitionManager"/> after all providers have defined their
/// templates; subsequent mutations are not expected.
/// </summary>
internal sealed class RoleTemplateContext : IRoleTemplateContext
{
    // Ordered to maintain registration order for predictable seeding.
    private readonly Dictionary<string, RoleTemplateBuilder> _builders =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public RoleTemplateBuilder AddTemplate(string key, string name, string? description = null)
    {
        if (_builders.ContainsKey(key))
            throw new InvalidOperationException(
                $"A role template with key '{key}' has already been registered. " +
                "Keys must be unique across all IRoleTemplateProvider implementations.");

        var builder = new RoleTemplateBuilder(key, name, description);
        _builders[key] = builder;
        return builder;
    }

    /// <summary>
    /// Materialises all registered builders into an immutable dictionary keyed by template key.
    /// </summary>
    internal Dictionary<string, RoleTemplateDefinition> Build() =>
        _builders.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Build(),
            StringComparer.OrdinalIgnoreCase);
}
