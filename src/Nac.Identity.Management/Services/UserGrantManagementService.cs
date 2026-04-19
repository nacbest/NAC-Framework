using Nac.Core.Abstractions.Identity;
using Nac.Core.Results;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;

namespace Nac.Identity.Management.Services;

/// <summary>
/// Manages direct (non-role) permission grants for a user within the current tenant.
/// Each mutation invalidates the affected user's permission cache key immediately.
/// </summary>
public sealed class UserGrantManagementService(
    IPermissionGrantRepository grantRepo,
    IPermissionGrantCache cache,
    ICurrentUser currentUser)
{
    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<string>>> ListGrantsAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result<IReadOnlyList<string>>.Forbidden("No active tenant.");

        var grants = await grantRepo.ListGrantsAsync(
            PermissionProviderNames.User, userId.ToString(), tenantId, ct);

        return Result<IReadOnlyList<string>>.Success(grants.OrderBy(p => p, StringComparer.Ordinal).ToList());
    }

    // ── Grant ─────────────────────────────────────────────────────────────────

    public async Task<Result> GrantAsync(Guid userId, GrantRequest request, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result.Forbidden("No active tenant.");

        await grantRepo.AddGrantAsync(
            PermissionProviderNames.User, userId.ToString(), request.PermissionName, tenantId, ct);

        await cache.InvalidateAsync(PermissionCacheKeys.User(userId, tenantId), ct);
        return Result.Success();
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    public async Task<Result> RevokeAsync(Guid userId, string permissionName, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Result.Forbidden("No active tenant.");

        await grantRepo.RemoveGrantAsync(
            PermissionProviderNames.User, userId.ToString(), permissionName, tenantId, ct);

        await cache.InvalidateAsync(PermissionCacheKeys.User(userId, tenantId), ct);
        return Result.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? RequireTenant() =>
        string.IsNullOrEmpty(currentUser.TenantId) ? null : currentUser.TenantId;
}
