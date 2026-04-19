using Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure;

/// <summary>EF Core mapping for <see cref="Customer"/> in the billing schema.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.TenantId).IsRequired().HasMaxLength(128);
        builder.Property(c => c.Plan).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        // One customer per user per tenant — prevents duplicate customer creation
        // if the handler is called concurrently for the same user+tenant.
        builder.HasIndex(c => new { c.UserId, c.TenantId }).IsUnique();
    }
}
