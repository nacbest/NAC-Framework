namespace Nac.Identity.Management.Contracts;

/// <summary>Role projection for list/detail endpoints.</summary>
public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsTemplate,
    Guid? BaseTemplateId,
    IReadOnlyList<string> Grants);

/// <summary>Create a new custom role in the current tenant.</summary>
public sealed record CreateRoleRequest(
    string Name,
    string? Description = null,
    IReadOnlyList<string>? InitialGrants = null);

/// <summary>Update role metadata (name / description). Grants managed separately.</summary>
public sealed record UpdateRoleRequest(string? Name, string? Description);

/// <summary>Clone a system template into the current tenant.</summary>
public sealed record CloneFromTemplateRequest(Guid TemplateRoleId, string Name, string? Description = null);
