using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nac.Identity.Permissions.Grants.Configurations;

/// <summary>
/// EF Core configuration for <see cref="PermissionGrant"/>. Provides the composite
/// lookup key for cache-key derivation and the scan index for warmup queries.
/// </summary>
internal sealed class PermissionGrantConfiguration : IEntityTypeConfiguration<PermissionGrant>
{
    public void Configure(EntityTypeBuilder<PermissionGrant> builder)
    {
        builder.ToTable("NacPermissionGrants");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.ProviderName)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(g => g.ProviderKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(g => g.PermissionName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(g => g.TenantId)
            .HasMaxLength(64);

        // Uniqueness — exact grant is idempotent.
        builder.HasIndex(g => new { g.ProviderName, g.ProviderKey, g.PermissionName, g.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_NacPermissionGrants_Provider_Permission_Tenant");

        // Warmup / introspection scans.
        builder.HasIndex(g => new { g.TenantId, g.ProviderName })
            .HasDatabaseName("IX_NacPermissionGrants_TenantId_ProviderName");
    }
}
