using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nac.Identity.Management.Authorization;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Management.Internal;
using Nac.Identity.Management.Services;

namespace Nac.Identity.Management.Controllers;

/// <summary>
/// Manages direct (non-role) permission grants for a specific user within the current tenant.
/// Direct grants are powerful; all endpoints require <c>Grants.Manage</c>.
/// </summary>
[ApiController]
[Route("api/identity/users/{userId:guid}/grants")]
public sealed class UserGrantsController(UserGrantManagementService service) : ControllerBase
{
    /// <summary>Lists direct permission grants for a user in the current tenant.</summary>
    [HttpGet]
    [Authorize(Policy = IdentityManagementPermissions.Grants_View)]
    public async Task<IActionResult> List(Guid userId, CancellationToken ct)
        => (await service.ListGrantsAsync(userId, ct)).ToActionResult(this);

    /// <summary>Grants a single permission directly to a user.</summary>
    [HttpPost]
    [Authorize(Policy = IdentityManagementPermissions.Grants_Manage)]
    public async Task<IActionResult> Grant(Guid userId, [FromBody] GrantRequest request, CancellationToken ct)
        => (await service.GrantAsync(userId, request, ct)).ToActionResult(this);

    /// <summary>Revokes a direct user permission grant.</summary>
    [HttpDelete("{permissionName}")]
    [Authorize(Policy = IdentityManagementPermissions.Grants_Manage)]
    public async Task<IActionResult> Revoke(Guid userId, string permissionName, CancellationToken ct)
        => (await service.RevokeAsync(userId, permissionName, ct)).ToActionResult(this);
}
