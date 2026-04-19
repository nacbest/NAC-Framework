using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Services;

namespace Nac.Identity.Permissions;

/// <summary>
/// ABP-style permission checker: resolves grants for (user + roles) via
/// <see cref="IPermissionGrantCache"/> → <see cref="IPermissionGrantRepository"/> and
/// walks the permission tree so ancestor grants satisfy descendant checks. JWT carries
/// no permission claims; this class is the sole authorization oracle at request time.
/// </summary>
internal sealed class PermissionChecker(
    IHttpContextAccessor httpContextAccessor,
    IPermissionGrantRepository repo,
    IPermissionGrantCache cache,
    PermissionDefinitionManager definitionManager,
    ILogger<PermissionChecker> logger) : IPermissionChecker
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    // ── Overloads ────────────────────────────────────────────────────────────

    public async Task<bool> IsGrantedAsync(string permissionName, CancellationToken ct = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return false;

        var (userId, tenantId, roleIds, _) = ReadClaims(user);
        if (userId == Guid.Empty) return false;

        var granted = await LoadGrantedAsync(userId, tenantId, roleIds, ct);
        return CheckWithTree(granted, permissionName);
    }

    public async Task<bool> IsGrantedAsync(Guid userId, string permissionName,
                                           string? tenantId = null, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return false;

        // Cross-user: evaluate direct user grants. Role-inclusive resolution requires a
        // membership lookup; a later phase can enrich this by injecting IMembershipService.
        var userGrants = await cache.GetOrLoadAsync(
            PermissionCacheKeys.User(userId, tenantId),
            ct2 => repo.ListGrantsAsync(PermissionProviderNames.User, userId.ToString(), tenantId, ct2),
            Ttl, ct);

        return CheckWithTree(userGrants, permissionName);
    }

    public Task<bool> IsGrantedAsync(string permissionName, string resourceType,
                                     string resourceId, CancellationToken ct = default)
    {
        logger.LogWarning("Resource-aware permission check deferred to v4 — falling back to name-only check for {Permission} on {Resource}:{Id}",
            permissionName, resourceType, resourceId);
        return IsGrantedAsync(permissionName, ct);
    }

    public async Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] permissionNames,
                                                                   CancellationToken ct = default)
    {
        var result = new MultiplePermissionGrantResult();
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            foreach (var p in permissionNames) result.SetResult(p, false);
            return result;
        }

        var (userId, tenantId, roleIds, _) = ReadClaims(user);
        var granted = userId == Guid.Empty
            ? new HashSet<string>(StringComparer.Ordinal)
            : await LoadGrantedAsync(userId, tenantId, roleIds, ct);

        foreach (var p in permissionNames)
            result.SetResult(p, CheckWithTree(granted, p));
        return result;
    }

    // ── Resolve ─────────────────────────────────────────────────────────────

    private async Task<HashSet<string>> LoadGrantedAsync(Guid userId, string? tenantId,
                                                        IReadOnlyList<Guid> roleIds,
                                                        CancellationToken ct)
    {
        var tasks = new List<Task<HashSet<string>>>(1 + roleIds.Count)
        {
            cache.GetOrLoadAsync(
                PermissionCacheKeys.User(userId, tenantId),
                ct2 => repo.ListGrantsAsync(PermissionProviderNames.User, userId.ToString(), tenantId, ct2),
                Ttl, ct),
        };

        foreach (var rid in roleIds)
        {
            tasks.Add(cache.GetOrLoadAsync(
                PermissionCacheKeys.Role(rid, tenantId),
                ct2 => repo.ListGrantsAsync(PermissionProviderNames.Role, rid.ToString(), tenantId, ct2),
                Ttl, ct));
        }

        var sets = await Task.WhenAll(tasks);
        var union = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in sets) union.UnionWith(s);
        return union;
    }

    private bool CheckWithTree(HashSet<string> granted, string permissionName)
    {
        if (granted.Contains(permissionName)) return true;

        var target = definitionManager.GetOrNull(permissionName);
        if (target is null) return false;

        foreach (var heldName in granted)
        {
            var heldDef = definitionManager.GetOrNull(heldName);
            if (heldDef is not null && IsDescendantOf(heldDef, permissionName))
                return true;
        }
        return false;
    }

    private static bool IsDescendantOf(PermissionDefinition ancestor, string targetName)
    {
        foreach (var child in ancestor.Children)
        {
            if (string.Equals(child.Name, targetName, StringComparison.Ordinal))
                return true;
            if (IsDescendantOf(child, targetName))
                return true;
        }
        return false;
    }

    // ── Claim extraction ─────────────────────────────────────────────────────

    private static (Guid userId, string? tenantId, IReadOnlyList<Guid> roleIds, bool isHost)
        ReadClaims(ClaimsPrincipal user)
    {
        var userId = Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : Guid.Empty;

        var tenantId = user.FindFirst(NacIdentityClaims.TenantId)?.Value;
        if (string.IsNullOrEmpty(tenantId)) tenantId = null;

        var roleIdsRaw = user.FindFirst(NacIdentityClaims.RoleIds)?.Value;
        IReadOnlyList<Guid> roleIds = string.IsNullOrEmpty(roleIdsRaw)
            ? Array.Empty<Guid>()
            : JsonSerializer.Deserialize<Guid[]>(roleIdsRaw) ?? Array.Empty<Guid>();

        var isHost = string.Equals(user.FindFirst(NacIdentityClaims.IsHost)?.Value, "true",
            StringComparison.OrdinalIgnoreCase);

        return (userId, tenantId, roleIds, isHost);
    }
}
