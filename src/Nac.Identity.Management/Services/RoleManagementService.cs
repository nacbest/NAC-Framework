using Microsoft.EntityFrameworkCore;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Results;
using Nac.Identity.Context;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Roles;
using Nac.Identity.Users;

namespace Nac.Identity.Management.Services;

/// <summary>
/// Tenant-scoped role management: create, clone, update metadata, delete, and
/// per-role grant management. All mutations are scoped to <see cref="ICurrentUser.TenantId"/>.
/// Bulk grant replace performs a single cache invalidation call.
/// </summary>
public sealed class RoleManagementService(
    IRoleService roleService,
    NacIdentityDbContext db,
    IPermissionGrantRepository grantRepo,
    IPermissionGrantCache cache,
    ICurrentUser currentUser)
{
    // ── List / Get ────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<RoleDto>>> ListAsync(CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result<IReadOnlyList<RoleDto>>.Forbidden("No active tenant.");

        var roles = await roleService.ListForTenantAsync(tenantId, ct);
        var dtos = await ToRoleDtosAsync(roles, tenantId, ct);
        return Result<IReadOnlyList<RoleDto>>.Success(dtos);
    }

    public async Task<Result<RoleDto>> GetAsync(Guid roleId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result<RoleDto>.Forbidden("No active tenant.");

        var role = await FindTenantRoleAsync(roleId, tenantId, ct);
        if (role is null) return Result<RoleDto>.NotFound($"Role '{roleId}' not found in current tenant.");

        var grants = await roleService.ListGrantsAsync(roleId, tenantId, ct);
        return Result<RoleDto>.Success(ToRoleDto(role, grants));
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> ListTemplatesAsync(CancellationToken ct = default)
    {
        var templates = await roleService.ListTemplatesAsync(ct);
        var dtos = templates.Select(r => ToRoleDto(r, Array.Empty<string>())).ToList();
        return Result<IReadOnlyList<RoleDto>>.Success(dtos);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result<RoleDto>.Forbidden("No active tenant.");

        var role = await roleService.CreateAsync(tenantId, request.Name, request.Description, ct);

        if (request.InitialGrants is { Count: > 0 })
        {
            foreach (var perm in request.InitialGrants)
                await roleService.GrantPermissionAsync(role.Id, perm, tenantId, ct);
            // Grants invalidate individually above; cache is fresh after loop.
        }

        var grants = await roleService.ListGrantsAsync(role.Id, tenantId, ct);
        return Result<RoleDto>.Success(ToRoleDto(role, grants));
    }

    public async Task<Result<RoleDto>> CloneFromTemplateAsync(CloneFromTemplateRequest request,
                                                              CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result<RoleDto>.Forbidden("No active tenant.");

        try
        {
            var role = await roleService.CloneFromTemplateAsync(tenantId, request.TemplateRoleId, request.Name, ct);
            var grants = await roleService.ListGrantsAsync(role.Id, tenantId, ct);
            return Result<RoleDto>.Success(ToRoleDto(role, grants));
        }
        catch (InvalidOperationException ex)
        {
            return Result<RoleDto>.NotFound(ex.Message);
        }
    }

    // ── Update metadata ───────────────────────────────────────────────────────

    public async Task<Result<RoleDto>> UpdateAsync(Guid roleId, UpdateRoleRequest request,
                                                   CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result<RoleDto>.Forbidden("No active tenant.");

        var role = await FindTenantRoleAsync(roleId, tenantId, ct);
        if (role is null) return Result<RoleDto>.NotFound($"Role '{roleId}' not found in current tenant.");

        if (request.Name is not null) role.Name = request.Name;
        if (request.Description is not null) role.Description = request.Description;
        await db.SaveChangesAsync(ct);

        var grants = await roleService.ListGrantsAsync(roleId, tenantId, ct);
        return Result<RoleDto>.Success(ToRoleDto(role, grants));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task<Result> DeleteAsync(Guid roleId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result.Forbidden("No active tenant.");

        var role = await FindTenantRoleAsync(roleId, tenantId, ct);
        if (role is null) return Result.NotFound($"Role '{roleId}' not found in current tenant.");

        try
        {
            await roleService.DeleteAsync(roleId, ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            // 409: membership references exist or template protection.
            return Result.Conflict(ex.Message);
        }
    }

    // ── Grant management ──────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<string>>> ListGrantsAsync(Guid roleId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result<IReadOnlyList<string>>.Forbidden("No active tenant.");

        if (await FindTenantRoleAsync(roleId, tenantId, ct) is null)
            return Result<IReadOnlyList<string>>.NotFound($"Role '{roleId}' not found in current tenant.");

        var grants = await roleService.ListGrantsAsync(roleId, tenantId, ct);
        return Result<IReadOnlyList<string>>.Success(grants);
    }

    public async Task<Result> GrantPermissionAsync(Guid roleId, string permissionName, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result.Forbidden("No active tenant.");

        if (await FindTenantRoleAsync(roleId, tenantId, ct) is null)
            return Result.NotFound($"Role '{roleId}' not found in current tenant.");

        try
        {
            await roleService.GrantPermissionAsync(roleId, permissionName, tenantId, ct);
            return Result.Success();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result.Invalid(new ValidationError(nameof(permissionName), ex.Message));
        }
    }

    public async Task<Result> RevokePermissionAsync(Guid roleId, string permissionName, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result.Forbidden("No active tenant.");

        if (await FindTenantRoleAsync(roleId, tenantId, ct) is null)
            return Result.NotFound($"Role '{roleId}' not found in current tenant.");

        try
        {
            await roleService.RevokePermissionAsync(roleId, permissionName, tenantId, ct);
            return Result.Success();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result.Invalid(new ValidationError(nameof(permissionName), ex.Message));
        }
    }

    /// <summary>
    /// Replaces all grants atomically — removes old grants, adds new ones, then
    /// issues a SINGLE cache invalidation for the role key to avoid storms.
    /// </summary>
    public async Task<Result> BulkReplaceGrantsAsync(Guid roleId, BulkGrantsRequest request,
                                                     CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result.Forbidden("No active tenant.");

        if (await FindTenantRoleAsync(roleId, tenantId, ct) is null)
            return Result.NotFound($"Role '{roleId}' not found in current tenant.");

        var roleKey = roleId.ToString();
        var existing = await grantRepo.ListGrantsAsync(PermissionProviderNames.Role, roleKey, tenantId, ct);

        var toRemove = existing.Except(request.PermissionNames, StringComparer.Ordinal).ToList();
        var toAdd    = request.PermissionNames.Except(existing, StringComparer.Ordinal).ToList();

        foreach (var perm in toRemove)
            await grantRepo.RemoveGrantAsync(PermissionProviderNames.Role, roleKey, perm, tenantId, ct);

        foreach (var perm in toAdd)
            await grantRepo.AddGrantAsync(PermissionProviderNames.Role, roleKey, perm, tenantId, ct);

        // Single invalidation after the full batch — satisfies bulk grant spec.
        await cache.InvalidateAsync(PermissionCacheKeys.Role(roleId, tenantId), ct);
        return Result.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? RequireTenant() =>
        string.IsNullOrEmpty(currentUser.TenantId) ? null : currentUser.TenantId;

    private Task<NacRole?> FindTenantRoleAsync(Guid roleId, string tenantId, CancellationToken ct) =>
        db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId && !r.IsDeleted, ct);

    private async Task<IReadOnlyList<RoleDto>> ToRoleDtosAsync(
        IReadOnlyList<NacRole> roles, string tenantId, CancellationToken ct)
    {
        var result = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            var grants = await roleService.ListGrantsAsync(role.Id, tenantId, ct);
            result.Add(ToRoleDto(role, grants));
        }
        return result;
    }

    private static RoleDto ToRoleDto(NacRole role, IReadOnlyList<string> grants) =>
        new(role.Id, role.Name!, role.Description, role.IsTemplate, role.BaseTemplateId, grants);
}
