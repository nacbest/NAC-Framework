using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nac.Identity.Management.Authorization;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Permissions;

namespace Nac.Identity.Management.Controllers;

/// <summary>
/// Read-only permission tree endpoint. Auth-gated to prevent anonymous enumeration.
/// Sourced from <see cref="PermissionDefinitionManager"/> — reflects all registered providers.
/// </summary>
[ApiController]
[Route("api/identity/permissions")]
public sealed class PermissionsController(PermissionDefinitionManager definitionManager) : ControllerBase
{
    /// <summary>Returns the full hierarchical permission tree across all registered providers.</summary>
    [HttpGet]
    [Authorize(Policy = IdentityManagementPermissions.Permissions_View)]
    public IActionResult GetTree()
    {
        var groups = definitionManager.GetGroups()
            .Select(g => new PermissionGroupDto(g.Name, g.DisplayName, MapNodes(g.Permissions)))
            .ToList();

        return Ok(groups);
    }

    private static IReadOnlyList<PermissionNodeDto> MapNodes(
        IEnumerable<Nac.Core.Abstractions.Permissions.PermissionDefinition> permissions) =>
        permissions.Select(p => new PermissionNodeDto(p.Name, p.DisplayName, MapNodes(p.Children))).ToList();
}
