using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nac.Identity.Entities;

namespace Nac.Identity.Data.Configurations;

internal sealed class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> builder)
    {
        builder.ToTable("TenantRoles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(64);

        // Store permissions as JSON array
        builder.Property(x => x.Permissions)
            .HasColumnType("jsonb");

        // Unique constraint: one role name per tenant
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique();

        // Index for tenant lookup
        builder.HasIndex(x => x.TenantId);
    }
}
