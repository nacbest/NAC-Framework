namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// Well-known HTTP header names used for tenant resolution.
/// </summary>
public static class NacTenantHeaders
{
    /// <summary>HTTP header carrying the tenant identifier.</summary>
    public const string TenantId = "X-Tenant-Id";
}
