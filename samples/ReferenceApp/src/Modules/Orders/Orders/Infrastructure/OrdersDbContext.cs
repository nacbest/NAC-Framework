using Microsoft.EntityFrameworkCore;
using Nac.Persistence.Context;
using Nac.Persistence.Outbox;
using Orders.Domain;

namespace Orders.Infrastructure;

/// <summary>
/// EF Core context for the Orders module.
/// Schema: "orders" — migrations history also lives in orders schema.
/// Inherits interceptor wiring (soft-delete filter, outbox, audit) from <see cref="NacDbContext"/>.
/// <see cref="OutboxEvent"/> is included so OutboxInterceptor can write rows
/// within the same transaction as business entity saves.
/// </summary>
internal sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : NacDbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Outbox events written by <c>OutboxInterceptor</c> in-transaction.</summary>
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default schema before base call so all tables land in "orders" schema.
        modelBuilder.HasDefaultSchema("orders");

        // Base applies ApplyConfigurationsFromAssembly(this assembly) + soft-delete filters.
        base.OnModelCreating(modelBuilder);

        // OutboxEventConfiguration is internal in Nac.Persistence — replicate constraints here
        // so outbox table gets correct column constraints and polling index.
        modelBuilder.Entity<OutboxEvent>(outbox =>
        {
            outbox.ToTable("__outbox_events");
            outbox.HasKey(o => o.Id);
            outbox.Property(o => o.EventType).IsRequired().HasMaxLength(512);
            outbox.Property(o => o.Payload).IsRequired();
            outbox.Property(o => o.Error).HasMaxLength(4000);
            // ── Audit / impersonation context (nullable — non-breaking) ──────
            outbox.Property(o => o.TenantId).HasMaxLength(256).IsRequired(false);
            outbox.Property(o => o.ActorUserId).IsRequired(false);
            outbox.Property(o => o.ImpersonatorUserId).IsRequired(false);
            // Composite index for OutboxWorker polling: WHERE ProcessedAt IS NULL ORDER BY CreatedAt
            outbox.HasIndex(o => new { o.ProcessedAt, o.CreatedAt });
        });
    }
}
