using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nac.Identity.Entities;

namespace Nac.Identity.Data.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.DeviceInfo)
            .HasMaxLength(512);

        // User relationship
        builder.HasOne(x => x.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for token lookup
        builder.HasIndex(x => x.TokenHash);

        // Index for cleanup of expired tokens
        builder.HasIndex(x => x.ExpiresAt);

        // Index for user's active tokens
        builder.HasIndex(x => new { x.UserId, x.RevokedAt });
    }
}
