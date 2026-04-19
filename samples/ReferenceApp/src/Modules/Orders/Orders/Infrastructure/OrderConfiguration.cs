using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain;

namespace Orders.Infrastructure;

/// <summary>EF Core fluent mapping for the <see cref="Order"/> aggregate.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.Total)
            .IsRequired()
            .HasColumnType("numeric(18,4)");

        builder.Property(o => o.TenantId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        // OrderItem is an owned collection — stored in a separate "order_items" table.
        builder.OwnsMany(o => o.Items, items =>
        {
            items.ToTable("order_items");

            // Surrogate PK for the join table row.
            items.WithOwner().HasForeignKey("OrderId");
            items.Property<Guid>("Id").ValueGeneratedOnAdd();
            items.HasKey("Id");

            items.Property(i => i.ProductId).IsRequired();

            items.Property(i => i.Quantity).IsRequired();

            items.Property(i => i.UnitPrice)
                .IsRequired()
                .HasColumnType("numeric(18,4)");

            // LineTotal is computed in C# — not persisted.
            items.Ignore(i => i.LineTotal);
        });

        // Index for tenant-scoped queries.
        builder.HasIndex(o => o.TenantId);

        // Index for customer order lookups.
        builder.HasIndex(o => o.CustomerId);
    }
}
