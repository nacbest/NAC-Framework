using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nac.Identity.Users;

namespace Nac.Identity.Memberships.Configurations;

/// <summary>
/// EF Core configuration for <see cref="UserTenantMembership"/>.
/// </summary>
internal sealed class UserTenantMembershipConfiguration : IEntityTypeConfiguration<UserTenantMembership>
{
    public void Configure(EntityTypeBuilder<UserTenantMembership> builder)
    {
        builder.ToTable("NacUserTenantMemberships");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<int>();

        // Unique: a user has at most one membership row per tenant.
        builder.HasIndex(m => new { m.UserId, m.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_NacUserTenantMemberships_UserId_TenantId");

        builder.HasIndex(m => m.TenantId)
            .HasDatabaseName("IX_NacUserTenantMemberships_TenantId");

        builder.HasOne<NacUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Roles)
            .WithOne()
            .HasForeignKey(r => r.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
