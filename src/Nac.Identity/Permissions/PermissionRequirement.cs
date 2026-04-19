using Microsoft.AspNetCore.Authorization;

namespace Nac.Identity.Permissions;

/// <summary>
/// Authorization requirement that demands a specific named permission.
/// Evaluated by <see cref="PermissionAuthorizationHandler"/>.
/// </summary>
public sealed class PermissionRequirement(string permissionName) : IAuthorizationRequirement
{
    public string PermissionName { get; } = permissionName;
}
