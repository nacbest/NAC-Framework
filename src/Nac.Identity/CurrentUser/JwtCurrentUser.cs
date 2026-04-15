using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nac.Abstractions.Auth;
using Nac.Abstractions.MultiTenancy;
using Nac.Identity.Data;

namespace Nac.Identity.CurrentUser;

/// <summary>
/// JWT-based implementation of <see cref="ICurrentUser"/>.
/// Reads identity from JWT claims, loads tenant-scoped permissions from database.
/// </summary>
public sealed class JwtCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;
    private readonly NacIdentityDbContext _dbContext;

    private HashSet<string>? _permissionsCache;
    private volatile bool _permissionsLoaded;
    private readonly object _lock = new();

    public JwtCurrentUser(
        IHttpContextAccessor httpContextAccessor,
        ITenantContext tenantContext,
        NacIdentityDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
        _dbContext = dbContext;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? Principal?.FindFirstValue("sub");

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name)
                            ?? Principal?.FindFirstValue("name");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true
                                && UserId is not null;

    public IReadOnlySet<string> Permissions
    {
        get
        {
            EnsurePermissionsLoaded();
            return _permissionsCache ?? new HashSet<string>();
        }
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return false;

        EnsurePermissionsLoaded();

        if (_permissionsCache is null || _permissionsCache.Count == 0)
            return false;

        // Exact match
        if (_permissionsCache.Contains(permission))
            return true;

        // Wildcard matching: "orders.*" matches "orders.create"
        foreach (var p in _permissionsCache)
        {
            if (MatchesWildcard(p, permission))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a wildcard permission pattern matches the requested permission.
    /// Supports: "module.*" matches "module.anything"
    ///           "*.action" matches "anything.action"
    /// </summary>
    private static bool MatchesWildcard(string pattern, string permission)
    {
        // "orders.*" → prefix "orders."
        if (pattern.EndsWith(".*"))
        {
            var prefix = pattern[..^1]; // "orders."
            if (permission.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        // "*.create" → suffix ".create"
        if (pattern.StartsWith("*."))
        {
            var suffix = pattern[1..]; // ".create"
            if (permission.EndsWith(suffix, StringComparison.Ordinal))
                return true;
        }

        // Full wildcard "*" matches everything
        if (pattern == "*")
            return true;

        return false;
    }

    /// <summary>
    /// Preloads permissions asynchronously. Call from middleware before handlers run
    /// to avoid sync-over-async DB calls in the <see cref="Permissions"/> property.
    /// </summary>
    internal async Task LoadPermissionsAsync(CancellationToken ct = default)
    {
        if (_permissionsLoaded)
            return;

        if (!IsAuthenticated)
        {
            _permissionsCache = [];
            _permissionsLoaded = true;
            return;
        }

        var userId = Guid.Parse(UserId!);
        var tenantId = _tenantContext.TenantId;

        if (string.IsNullOrEmpty(tenantId))
        {
            _permissionsCache = [];
            _permissionsLoaded = true;
            return;
        }

        // Async query: membership → role → permissions
        var membership = await _dbContext.TenantMemberships
            .Include(m => m.TenantRole)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId, ct);

        _permissionsCache = membership?.TenantRole?.Permissions is null
            ? []
            : [.. membership.TenantRole.Permissions];

        _permissionsLoaded = true;
    }

    private void EnsurePermissionsLoaded()
    {
        if (_permissionsLoaded)
            return;

        // Fallback: if LoadPermissionsAsync wasn't called by middleware,
        // use sync path with GetAwaiter().GetResult() as last resort.
        // This should not happen in normal flow — middleware preloads permissions.
        lock (_lock)
        {
            if (_permissionsLoaded)
                return;

            _permissionsCache = LoadPermissionsFallback();
            _permissionsLoaded = true;
        }
    }

    private HashSet<string> LoadPermissionsFallback()
    {
        if (!IsAuthenticated)
            return [];

        var userId = Guid.Parse(UserId!);
        var tenantId = _tenantContext.TenantId;

        if (string.IsNullOrEmpty(tenantId))
            return [];

        // Sync fallback — prefer LoadPermissionsAsync in middleware
        var membership = _dbContext.TenantMemberships
            .Include(m => m.TenantRole)
            .FirstOrDefault(m => m.UserId == userId && m.TenantId == tenantId);

        if (membership?.TenantRole?.Permissions is null)
            return [];

        return [.. membership.TenantRole.Permissions];
    }
}
