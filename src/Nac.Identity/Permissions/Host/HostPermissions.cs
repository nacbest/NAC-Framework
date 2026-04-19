namespace Nac.Identity.Permissions.Host;

/// <summary>Host-level permission constants.</summary>
public static class HostPermissions
{
    /// <summary>Allows the bearer to query across all tenants via IgnoreQueryFilters.</summary>
    public const string AccessAllTenants = "Host.AccessAllTenants";
}
