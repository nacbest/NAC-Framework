namespace Nac.Identity.Services;

/// <summary>
/// Custom claim type constants used by NAC Framework identity services. Pattern A
/// minimal JWT carries only: <c>sub, email, name, tenant_id?, role_ids?, is_host?</c>.
/// Permission claims are NOT embedded — <c>PermissionChecker</c> resolves grants
/// via cache→DB at request time.
/// </summary>
public static class NacIdentityClaims
{
    /// <summary>Claim type for the currently selected tenant slug.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Claim type for the JSON-serialised array of role ids (Guid[]).</summary>
    public const string RoleIds = "role_ids";

    /// <summary>Claim type flagging a host (platform) account.</summary>
    public const string IsHost = "is_host";

    /// <summary>Claim type for a permission entry (legacy — retained for compatibility).</summary>
    public const string Permission = "permission";
}
