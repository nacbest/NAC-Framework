namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Singleton that builds and caches the full role template registry by calling every
/// registered <see cref="IRoleTemplateProvider"/> once at construction time. Throws on
/// duplicate keys so misconfiguration is caught at startup, not at runtime.
/// </summary>
public sealed class RoleTemplateDefinitionManager
{
    private readonly Dictionary<string, RoleTemplateDefinition> _definitions;

    /// <param name="providers">All registered <see cref="IRoleTemplateProvider"/> implementations.</param>
    /// <exception cref="InvalidOperationException">Thrown when two providers register the same template key.</exception>
    public RoleTemplateDefinitionManager(IEnumerable<IRoleTemplateProvider> providers)
    {
        var context = new RoleTemplateContext();

        foreach (var provider in providers)
            provider.Define(context);

        _definitions = context.Build();
    }

    /// <summary>Returns every registered template definition.</summary>
    public IReadOnlyCollection<RoleTemplateDefinition> GetAll() =>
        _definitions.Values;

    /// <summary>Returns the definition for <paramref name="key"/>, or <c>null</c> if not registered.</summary>
    public RoleTemplateDefinition? Get(string key) =>
        _definitions.TryGetValue(key, out var def) ? def : null;

    /// <summary>Returns <c>true</c> when a definition exists for <paramref name="key"/>.</summary>
    public bool Contains(string key) => _definitions.ContainsKey(key);
}
