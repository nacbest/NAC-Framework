namespace Nac.Identity.Management.Contracts;

/// <summary>Single node in the hierarchical permission tree.</summary>
public sealed record PermissionNodeDto(
    string Name,
    string? DisplayName,
    IReadOnlyList<PermissionNodeDto> Children);

/// <summary>Top-level permission group containing a tree of permission nodes.</summary>
public sealed record PermissionGroupDto(
    string Name,
    string? DisplayName,
    IReadOnlyList<PermissionNodeDto> Permissions);
