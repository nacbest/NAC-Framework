namespace Nac.MultiTenancy.Management.Abstractions;

/// <summary>
/// Strategy used to isolate a tenant's data from other tenants.
/// </summary>
public enum TenantIsolationMode
{
    /// <summary>Shared database for all tenants; rows discriminated by tenant id (default).</summary>
    Shared = 0,

    /// <summary>Dedicated database per tenant; resolved via per-tenant connection string.</summary>
    Database = 1,
}
