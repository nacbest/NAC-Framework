using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nac.Identity.Impersonation.Configurations;

/// <summary>EF Core configuration for <see cref="ImpersonationSession"/>.</summary>
internal sealed class ImpersonationSessionConfiguration : IEntityTypeConfiguration<ImpersonationSession>
{
    public void Configure(EntityTypeBuilder<ImpersonationSession> builder)
    {
        builder.ToTable("NacImpersonationSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Reason).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Jti).HasMaxLength(64).IsRequired();

        builder.HasIndex(s => s.Jti).IsUnique().HasDatabaseName("IX_NacImpersonationSessions_Jti");
        builder.HasIndex(s => new { s.HostUserId, s.IssuedAt })
               .HasDatabaseName("IX_NacImpersonationSessions_HostUserId_IssuedAt");

        builder.Ignore(s => s.DomainEvents);
    }
}
