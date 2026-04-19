using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nac.Identity.Management.Authorization;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Management.Internal;
using Nac.Identity.Management.Services;

namespace Nac.Identity.Management.Controllers;

/// <summary>
/// Manages tenant membership lifecycle: invite, accept, list, remove, and role reassignment.
/// All endpoints are scoped to the authenticated caller's current tenant.
/// </summary>
[ApiController]
[Route("api/identity/memberships")]
public sealed class MembershipsController(MembershipManagementService service) : ControllerBase
{
    /// <summary>Invites a user (new or existing) to the current tenant.</summary>
    [HttpPost("invite")]
    [Authorize(Policy = IdentityManagementPermissions.Memberships_Manage)]
    public async Task<IActionResult> Invite([FromBody] InviteRequest request, CancellationToken ct)
        => (await service.InviteAsync(request, ct)).ToActionResult(this);

    /// <summary>Lists all memberships in the current tenant.</summary>
    [HttpGet]
    [Authorize(Policy = IdentityManagementPermissions.Memberships_View)]
    public async Task<IActionResult> List(CancellationToken ct)
        => (await service.ListAsync(ct)).ToActionResult(this);

    /// <summary>Removes a member from the current tenant (soft-delete).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = IdentityManagementPermissions.Memberships_Manage)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
        => (await service.RemoveAsync(id, ct)).ToActionResult(this);

    /// <summary>Replaces the role assignments on a membership. Invalidates user permission cache.</summary>
    [HttpPatch("{id:guid}/roles")]
    [Authorize(Policy = IdentityManagementPermissions.Memberships_Manage)]
    public async Task<IActionResult> ChangeRoles(Guid id, [FromBody] ChangeMembershipRolesRequest request,
                                                 CancellationToken ct)
        => (await service.ChangeRolesAsync(id, request, ct)).ToActionResult(this);
}
