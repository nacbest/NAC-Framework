using Microsoft.EntityFrameworkCore;
using Nac.MultiTenancy.Management.Domain;
using Nac.Persistence.Context;
using Nac.Persistence.Outbox;

namespace Nac.MultiTenancy.Management.Persistence;

/// <summary>
/// Central registry <see cref="DbContext"/> for tenant aggregates. Inherits
/// <see cref="NacDbContext"/> directly (NOT a multi-tenant context) — the registry
/// itself is a host-level resource and must not apply per-tenant query filters.
/// Outbox, audit, and soft-delete interceptors are wired by the consumer via
/// <c>AddNacPersistence&lt;TenantManagementDbContext&gt;()</c>.
/// </summary>
public class TenantManagementDbContext : NacDbContext
{
    /// <summary>Tenant aggregates.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Outbox table — required when the outbox interceptor is enabled.</summary>
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    /// <summary>
    /// Initialises a new instance of <see cref="TenantManagementDbContext"/>.
    /// </summary>
    public TenantManagementDbContext(DbContextOptions<TenantManagementDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Base class auto-applies IEntityTypeConfiguration<T> from this assembly,
        // including TenantConfiguration. Explicitly map outbox table here too —
        // OutboxEventConfiguration lives in Nac.Persistence assembly, so we apply manually.
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutboxEvent).Assembly);
    }
}
