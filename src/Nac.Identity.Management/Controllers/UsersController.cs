using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Results;
using Nac.Identity.Context;
using Nac.Identity.Management.Authorization;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Management.Internal;
using Nac.Identity.Memberships;
using Nac.Identity.Users;

namespace Nac.Identity.Management.Controllers;

/// <summary>
/// Manages users visible within the current tenant context.
/// Users are global (Pattern A); this controller surfaces only those with active memberships.
/// </summary>
[ApiController]
[Route("api/identity/users")]
public sealed class UsersController(
    NacIdentityDbContext db,
    UserManager<NacUser> userManager,
    IMembershipService membershipService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Lists users with an active or invited membership in the current tenant (paged).</summary>
    [HttpGet]
    [Authorize(Policy = IdentityManagementPermissions.Users_View)]
    public async Task<IActionResult> List([FromQuery] UserListQuery query, CancellationToken ct)
    {
        var tenantId = currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId)) return Forbid();

        var members = await membershipService.ListMembersAsync(tenantId, ct);
        var userIds = members.Select(m => m.UserId).Distinct().ToList();

        IQueryable<NacUser> q = db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            q = q.Where(u => (u.Email != null && u.Email.ToLower().Contains(s))
                          || (u.FullName != null && u.FullName.ToLower().Contains(s)));
        }

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);
        var items = await q.OrderBy(u => u.Email)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        var dtos = items.Select(u => new UserSummaryDto(u.Id, u.Email, u.FullName, u.IsActive, u.IsHost)).ToList();
        return Ok(dtos);
    }

    /// <summary>Returns user detail including their memberships in the current tenant.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = IdentityManagementPermissions.Users_View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId)) return Forbid();

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound($"User '{id}' not found.");

        var memberships = await membershipService.ListMembersAsync(tenantId, ct);
        var tenantMemberships = memberships.Where(m => m.UserId == id).Select(m =>
            new MembershipDto(m.Id, m.UserId, user.Email, m.TenantId,
                m.Status.ToString(), m.RoleIds, m.JoinedAt, m.IsDefault)).ToList();

        var dto = new UserDetailDto(user.Id, user.Email, user.FullName, user.IsActive, user.IsHost, tenantMemberships);
        return Ok(dto);
    }

    /// <summary>Soft-disables a user globally. Restricted to host admins.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = IdentityManagementPermissions.Users_Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        if (!currentUser.IsHost) return Forbid();

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound($"User '{id}' not found.");

        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? NoContent()
            : Result.Invalid(result.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToArray())
                    .ToActionResult(this);
    }
}
