namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Immutable descriptor for a system role template. Instances are built by
/// <see cref="IRoleTemplateProvider"/> implementations via the fluent
/// <see cref="RoleTemplateBuilder"/> and stored in <see cref="RoleTemplateDefinitionManager"/>.
/// </summary>
/// <param name="Key">Stable lowercase identifier (e.g. "owner", "admin").</param>
/// <param name="Name">Human-readable display name.</param>
/// <param name="Description">Optional description of the template's purpose.</param>
/// <param name="PermissionNames">Ordered, deduplicated list of permissions seeded for this template.</param>
public sealed record RoleTemplateDefinition(
    string Key,
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionNames);
