using Microsoft.EntityFrameworkCore;

namespace Nac.MultiTenancy.Management.Abstractions;

/// <summary>
/// Configuration knobs for the tenant management module. Supplied by the consumer
/// via <c>AddNacTenantManagement(opts =&gt; ...)</c>.
/// </summary>
public sealed class TenantManagementOptions
{
    /// <summary>EF Core provider configuration callback (e.g. <c>UseNpgsql</c>).</summary>
    internal Action<DbContextOptionsBuilder>? DbContextConfigure { get; private set; }

    /// <summary>
    /// Informational route prefix surfaced in the OpenAPI doc. Hard-coded in v1
    /// because MVC routing requires compile-time attributes.
    /// </summary>
    public string RoutePrefix { get; set; } = "/api/admin/tenants";

    /// <summary>Authorization policy + claim name guarding the controller.</summary>
    public string PermissionName { get; set; } = "Tenants.Manage";

    /// <summary>Default page size when the caller omits one.</summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>Hard ceiling on page size; protects against overly broad queries.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Maximum number of ids accepted by a single bulk endpoint call.</summary>
    public int MaxBulkSize { get; set; } = 100;

    /// <summary>Registers the EF Core provider for <c>TenantManagementDbContext</c>.</summary>
    public TenantManagementOptions UseDbContext(Action<DbContextOptionsBuilder> configure)
    {
        DbContextConfigure = configure;
        return this;
    }
}
