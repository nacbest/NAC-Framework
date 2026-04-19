namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Mutable context passed to <see cref="IRoleTemplateProvider.Define"/> during the
/// template registration phase. Use the fluent <see cref="RoleTemplateBuilder"/> returned
/// by <see cref="AddTemplate"/> to attach permission names.
/// </summary>
public interface IRoleTemplateContext
{
    /// <summary>
    /// Registers a new template with the given <paramref name="key"/>.
    /// Duplicate keys throw at startup (fail-fast).
    /// </summary>
    /// <param name="key">Stable lowercase identifier (e.g. "owner", "admin").</param>
    /// <param name="name">Human-readable display name.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A fluent builder to attach <c>Grants(...)</c> calls.</returns>
    RoleTemplateBuilder AddTemplate(string key, string name, string? description = null);
}
