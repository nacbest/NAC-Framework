using Nac.Identity.Permissions.Grants;

namespace Nac.Identity.Permissions.Cache;

/// <summary>
/// Helpers to build distributed cache keys for permission grants. Shape
/// <c>nac:perm:pn={providerName}:pk={providerKey}:t={tenantId|_}</c>.
/// </summary>
public static class PermissionCacheKeys
{
    private const string TenantlessMarker = "_";

    public static string User(Guid userId, string? tenantId) =>
        Build(PermissionProviderNames.User, userId.ToString(), tenantId);

    public static string Role(Guid roleId, string? tenantId) =>
        Build(PermissionProviderNames.Role, roleId.ToString(), tenantId);

    public static string Build(string providerName, string providerKey, string? tenantId) =>
        $"nac:perm:pn={providerName}:pk={providerKey}:t={tenantId ?? TenantlessMarker}";

    /// <summary>Pattern matching all keys for a given provider tuple across tenants.</summary>
    public static string RolePattern(Guid roleId) =>
        $"nac:perm:pn={PermissionProviderNames.Role}:pk={roleId}:t=*";

    public static string UserPattern(Guid userId) =>
        $"nac:perm:pn={PermissionProviderNames.User}:pk={userId}:t=*";
}
