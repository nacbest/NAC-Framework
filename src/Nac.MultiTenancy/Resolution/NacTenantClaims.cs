namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// Well-known JWT / identity claim names used for tenant resolution.
/// </summary>
public static class NacTenantClaims
{
    /// <summary>Claim type carrying the tenant identifier.</summary>
    public const string TenantId = "tenant_id";
}
