using Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Nac.Persistence.Context;
using Nac.Persistence.Outbox;

namespace Billing.Infrastructure;

/// <summary>
/// EF Core context for the Billing module.
/// Schema: "billing" — migrations history also lives in billing schema.
/// Inherits soft-delete filter and configuration scan from <see cref="NacDbContext"/>.
/// <see cref="OutboxEvent"/> is included so OutboxInterceptor can write rows
/// within the same transaction as business entity saves.
/// </summary>
internal sealed class BillingDbContext(DbContextOptions<BillingDbContext> options)
    : NacDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice>  Invoices  => Set<Invoice>();

    /// <summary>Outbox events written by <c>OutboxInterceptor</c> in-transaction.</summary>
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default schema before base call so all tables land in "billing" schema.
        modelBuilder.HasDefaultSchema("billing");

        // Base applies ApplyConfigurationsFromAssembly(this assembly) + soft-delete filters.
        base.OnModelCreating(modelBuilder);

        // Replicate OutboxEvent constraints (internal to Nac.Persistence) for billing schema.
        modelBuilder.Entity<OutboxEvent>(outbox =>
        {
            outbox.ToTable("__outbox_events");
            outbox.HasKey(o => o.Id);
            outbox.Property(o => o.EventType).IsRequired().HasMaxLength(512);
            outbox.Property(o => o.Payload).IsRequired();
            outbox.Property(o => o.Error).HasMaxLength(4000);
            // Composite index for OutboxWorker polling: WHERE ProcessedAt IS NULL ORDER BY CreatedAt
            outbox.HasIndex(o => new { o.ProcessedAt, o.CreatedAt });
        });
    }
}
