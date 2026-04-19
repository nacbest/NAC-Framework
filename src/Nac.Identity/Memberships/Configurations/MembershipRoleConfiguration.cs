using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nac.Identity.Users;

namespace Nac.Identity.Memberships.Configurations;

/// <summary>
/// EF Core configuration for <see cref="MembershipRole"/> (composite key).
/// </summary>
internal sealed class MembershipRoleConfiguration : IEntityTypeConfiguration<MembershipRole>
{
    public void Configure(EntityTypeBuilder<MembershipRole> builder)
    {
        builder.ToTable("NacMembershipRoles");
        builder.HasKey(r => new { r.MembershipId, r.RoleId });

        builder.HasOne<NacRole>()
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.RoleId)
            .HasDatabaseName("IX_NacMembershipRoles_RoleId");
    }
}
