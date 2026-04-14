using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nac.Identity.Entities;

namespace Nac.Identity.Data.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired()
            .HasMaxLength(64);

        // User relationship
        builder.HasOne(x => x.User)
            .WithMany(u => u.TenantMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // TenantRole relationship
        builder.HasOne(x => x.TenantRole)
            .WithMany(r => r.Memberships)
            .HasForeignKey(x => x.TenantRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one membership per user per tenant
        builder.HasIndex(x => new { x.UserId, x.TenantId })
            .IsUnique();

        // Index for tenant member lookup
        builder.HasIndex(x => x.TenantId);
    }
}
