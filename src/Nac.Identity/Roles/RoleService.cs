using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Users;

namespace Nac.Identity.Roles;

/// <summary>
/// Shell implementation of <see cref="IRoleService"/>. Phase 04 will enrich the clone
/// path with template-scoped grant copying; for now clone copies the row only.
/// </summary>
internal sealed class RoleService(
    RoleManager<NacRole> roleManager,
    NacIdentityDbContext db,
    IPermissionGrantRepository grantRepo,
    IPermissionGrantCache cache) : IRoleService
{
    public async Task<NacRole> CreateAsync(string tenantId, string name, string? description = null,
                                           CancellationToken ct = default)
    {
        var role = new NacRole(name, tenantId, isTemplate: false, description);
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to create role: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        return role;
    }

    public async Task<IReadOnlyList<NacRole>> ListForTenantAsync(string tenantId,
                                                                CancellationToken ct = default) =>
        await db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task DeleteAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null) return;

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to delete role: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        await cache.InvalidateByPatternAsync(PermissionCacheKeys.RolePattern(roleId), ct);
    }

    public async Task<NacRole> CloneFromTemplateAsync(string tenantId, Guid templateRoleId,
                                                     string? newName = null,
                                                     CancellationToken ct = default)
    {
        var template = await db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == templateRoleId && r.IsTemplate, ct)
            ?? throw new InvalidOperationException(
                $"Template role {templateRoleId} not found or is not a template.");

        var clone = new NacRole(newName ?? template.Name!, tenantId,
            isTemplate: false, description: template.Description);

        var result = await roleManager.CreateAsync(clone);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to clone role: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        // Copy template grants (tenantId=null) into the new tenant-scoped role.
        var templateGrants = await grantRepo.ListGrantsAsync(
            PermissionProviderNames.Role, templateRoleId.ToString(), tenantId: null, ct);
        foreach (var perm in templateGrants)
        {
            await grantRepo.AddGrantAsync(PermissionProviderNames.Role, clone.Id.ToString(),
                perm, tenantId, ct);
        }

        return clone;
    }

    public async Task GrantPermissionAsync(Guid roleId, string permissionName, string tenantId,
                                           CancellationToken ct = default)
    {
        await grantRepo.AddGrantAsync(PermissionProviderNames.Role, roleId.ToString(),
            permissionName, tenantId, ct);
        await cache.InvalidateAsync(PermissionCacheKeys.Role(roleId, tenantId), ct);
    }

    public async Task RevokePermissionAsync(Guid roleId, string permissionName, string tenantId,
                                            CancellationToken ct = default)
    {
        await grantRepo.RemoveGrantAsync(PermissionProviderNames.Role, roleId.ToString(),
            permissionName, tenantId, ct);
        await cache.InvalidateAsync(PermissionCacheKeys.Role(roleId, tenantId), ct);
    }
}
