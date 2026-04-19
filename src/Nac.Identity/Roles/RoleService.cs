using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Users;

namespace Nac.Identity.Roles;

/// <summary>
/// Tenant-scoped role management: create, clone from template, grant/revoke permissions,
/// list, and delete. Template rows (<c>IsTemplate=true</c>) are immutable at runtime —
/// only the seeder writes them.
/// </summary>
internal sealed class RoleService(
    RoleManager<NacRole> roleManager,
    NacIdentityDbContext db,
    IPermissionGrantRepository grantRepo,
    IPermissionGrantCache cache,
    PermissionDefinitionManager permManager) : IRoleService
{
    // ── Create ────────────────────────────────────────────────────────────────

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

    // ── Clone from template ───────────────────────────────────────────────────

    public async Task<NacRole> CloneFromTemplateAsync(string tenantId, Guid templateRoleId,
                                                      string? newName = null,
                                                      CancellationToken ct = default)
    {
        var template = await db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == templateRoleId && r.IsTemplate, ct)
            ?? throw new InvalidOperationException(
                $"Template role '{templateRoleId}' not found or is not a system template.");

        var clone = new NacRole(
            newName ?? template.Name!,
            tenantId,
            isTemplate: false,
            description: template.Description)
        {
            BaseTemplateId = templateRoleId,
        };

        // Load template grants (TenantId=null) before adding role to avoid
        // SaveChangesAsync being called by repo.AddGrantAsync internally.
        var templateGrants = await grantRepo.ListGrantsAsync(
            PermissionProviderNames.Role, templateRoleId.ToString(), tenantId: null, ct);

        // Add role and all cloned grants in a single unit of work.
        db.Roles.Add(clone);

        foreach (var perm in templateGrants)
        {
            db.PermissionGrants.Add(
                new PermissionGrant(PermissionProviderNames.Role, clone.Id.ToString(), perm, tenantId));
        }

        await db.SaveChangesAsync(ct);
        return clone;
    }

    // ── Grant / Revoke ────────────────────────────────────────────────────────

    public async Task GrantPermissionAsync(Guid roleId, string permissionName, string tenantId,
                                           CancellationToken ct = default)
    {
        await AssertMutableRoleAsync(roleId, ct);
        AssertKnownPermission(permissionName);

        await grantRepo.AddGrantAsync(PermissionProviderNames.Role, roleId.ToString(),
            permissionName, tenantId, ct);
        await cache.InvalidateAsync(PermissionCacheKeys.Role(roleId, tenantId), ct);
    }

    public async Task RevokePermissionAsync(Guid roleId, string permissionName, string tenantId,
                                            CancellationToken ct = default)
    {
        await AssertMutableRoleAsync(roleId, ct);

        await grantRepo.RemoveGrantAsync(PermissionProviderNames.Role, roleId.ToString(),
            permissionName, tenantId, ct);
        await cache.InvalidateAsync(PermissionCacheKeys.Role(roleId, tenantId), ct);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<NacRole>> ListForTenantAsync(string tenantId,
                                                                 CancellationToken ct = default) =>
        await db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && !r.IsDeleted)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ListGrantsAsync(Guid roleId, string? tenantId,
                                                             CancellationToken ct = default)
    {
        var grants = await grantRepo.ListGrantsAsync(
            PermissionProviderNames.Role, roleId.ToString(), tenantId, ct);
        return grants.OrderBy(p => p, StringComparer.Ordinal).ToList();
    }

    public async Task<IReadOnlyList<NacRole>> ListTemplatesAsync(CancellationToken ct = default) =>
        await db.Roles.AsNoTracking()
            .Where(r => r.IsTemplate && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null) return;

        if (role.IsTemplate)
            throw new InvalidOperationException(
                $"Role '{roleId}' is a system template and cannot be deleted.");

        // Reject deletion when any membership references this role (409-analogous).
        var isReferenced = await db.MembershipRoles.AnyAsync(mr => mr.RoleId == roleId, ct);
        if (isReferenced)
            throw new InvalidOperationException(
                $"Role '{roleId}' is currently assigned to one or more memberships and cannot be deleted. " +
                "Remove all membership assignments before deleting the role.");

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to delete role: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        await cache.InvalidateByPatternAsync(PermissionCacheKeys.RolePattern(roleId), ct);
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private async Task AssertMutableRoleAsync(Guid roleId, CancellationToken ct)
    {
        var isTemplate = await db.Roles
            .AsNoTracking()
            .Where(r => r.Id == roleId)
            .Select(r => (bool?)r.IsTemplate)
            .FirstOrDefaultAsync(ct);

        if (isTemplate is null)
            throw new KeyNotFoundException($"Role '{roleId}' not found.");

        if (isTemplate == true)
            throw new InvalidOperationException(
                $"Role '{roleId}' is a system template. " +
                "Template permissions are managed by IRoleTemplateProvider, not at runtime.");
    }

    private void AssertKnownPermission(string permissionName)
    {
        if (permManager.GetOrNull(permissionName) is null)
            throw new ArgumentException(
                $"Permission '{permissionName}' is not registered in PermissionDefinitionManager. " +
                "Check for typos or register the permission via IPermissionDefinitionProvider.",
                nameof(permissionName));
    }
}
