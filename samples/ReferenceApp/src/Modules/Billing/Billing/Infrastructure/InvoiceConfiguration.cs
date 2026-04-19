using Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure;

/// <summary>EF Core mapping for <see cref="Invoice"/> in the billing schema.</summary>
internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CustomerId).IsRequired();
        builder.Property(i => i.OrderId).IsRequired();
        builder.Property(i => i.Amount).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.TenantId).IsRequired().HasMaxLength(128);
        builder.Property(i => i.CreatedAt).IsRequired();

        // Unique index on OrderId enforces idempotency when OutboxWorker redelivers
        // OrderCreatedEvent — AnyAsync check in handler is the fast-path guard;
        // this index is the hard safety net at the DB layer.
        // NOTE: OrderId is a WEAK reference — no FK to orders schema (cross-schema FK anti-pattern).
        builder.HasIndex(i => i.OrderId).IsUnique();
    }
}
