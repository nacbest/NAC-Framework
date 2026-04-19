using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nac.Identity.Management.Authorization;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Management.Internal;
using Nac.Identity.Management.Services;

namespace Nac.Identity.Management.Controllers;

/// <summary>
/// Tenant-scoped role CRUD and per-role permission grant management.
/// Bulk grant replace (<c>PUT grants</c>) performs a single cache invalidation.
/// </summary>
[ApiController]
[Route("api/identity/roles")]
public sealed class RolesController(RoleManagementService service) : ControllerBase
{
    /// <summary>Lists roles in the current tenant.</summary>
    [HttpGet]
    [Authorize(Policy = IdentityManagementPermissions.Roles_View)]
    public async Task<IActionResult> List(CancellationToken ct)
        => (await service.ListAsync(ct)).ToActionResult(this);

    /// <summary>Returns role detail including its permission grants.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = IdentityManagementPermissions.Roles_View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await service.GetAsync(id, ct)).ToActionResult(this);

    /// <summary>Lists all system template roles available for cloning.</summary>
    [HttpGet("/api/identity/role-templates")]
    [Authorize(Policy = IdentityManagementPermissions.Roles_View)]
    public async Task<IActionResult> ListTemplates(CancellationToken ct)
        => (await service.ListTemplatesAsync(ct)).ToActionResult(this);

    /// <summary>Clones a system template into the current tenant.</summary>
    [HttpPost("from-template")]
    [Authorize(Policy = IdentityManagementPermissions.Roles_Manage)]
    public async Task<IActionResult> CloneFromTemplate([FromBody] CloneFromTemplateRequest request,
                                                       CancellationToken ct)
        => (await service.CloneFromTemplateAsync(request, ct)).ToActionResult(this);

    /// <summary>Creates a custom role in the current tenant.</summary>
    [HttpPost]
    [Authorize(Policy = IdentityManagementPermissions.Roles_Manage)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
        => (await service.CreateAsync(request, ct)).ToActionResult(this);

    /// <summary>Updates role name or description.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = IdentityManagementPermissions.Roles_Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
        => (await service.UpdateAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Soft-deletes a role. Returns 409 if membership references exist.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = IdentityManagementPermissions.Roles_Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await service.DeleteAsync(id, ct)).ToActionResult(this);

    /// <summary>Lists permission grants on a role.</summary>
    [HttpGet("{id:guid}/grants")]
    [Authorize(Policy = IdentityManagementPermissions.Grants_View)]
    public async Task<IActionResult> ListGrants(Guid id, CancellationToken ct)
        => (await service.ListGrantsAsync(id, ct)).ToActionResult(this);

    /// <summary>Grants a single permission to a role.</summary>
    [HttpPost("{id:guid}/grants")]
    [Authorize(Policy = IdentityManagementPermissions.Grants_Manage)]
    public async Task<IActionResult> Grant(Guid id, [FromBody] GrantRequest request, CancellationToken ct)
        => (await service.GrantPermissionAsync(id, request.PermissionName, ct)).ToActionResult(this);

    /// <summary>Revokes a single permission from a role.</summary>
    [HttpDelete("{id:guid}/grants/{permissionName}")]
    [Authorize(Policy = IdentityManagementPermissions.Grants_Manage)]
    public async Task<IActionResult> Revoke(Guid id, string permissionName, CancellationToken ct)
        => (await service.RevokePermissionAsync(id, permissionName, ct)).ToActionResult(this);

    /// <summary>Bulk-replaces all grants on a role — single cache invalidation.</summary>
    [HttpPut("{id:guid}/grants")]
    [Authorize(Policy = IdentityManagementPermissions.Grants_Manage)]
    public async Task<IActionResult> BulkReplaceGrants(Guid id, [FromBody] BulkGrantsRequest request,
                                                        CancellationToken ct)
        => (await service.BulkReplaceGrantsAsync(id, request, ct)).ToActionResult(this);
}
