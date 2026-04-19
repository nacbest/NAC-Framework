using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nac.Identity.Users;

namespace Nac.Identity.Roles.Configurations;

/// <summary>
/// EF Core configuration for <see cref="NacRole"/>. Adds tenant-scoping, template flag,
/// and a composite unique index on <c>(TenantId, NormalizedName)</c>. PostgreSQL treats
/// NULL as distinct, so multiple system templates (TenantId=null) may share names only
/// if explicitly allowed — here we rely on the IdentityRole NormalizedName convention.
/// </summary>
internal sealed class NacRoleConfiguration : IEntityTypeConfiguration<NacRole>
{
    public void Configure(EntityTypeBuilder<NacRole> builder)
    {
        builder.Property(r => r.TenantId)
            .HasMaxLength(64);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        // Lineage reference — nullable, no FK constraint (templates are NacRole rows
        // in the same table; self-referential FK would complicate seeder ordering).
        builder.Property(r => r.BaseTemplateId)
            .IsRequired(false);

        // Tenant-scoped role uniqueness. NULL TenantId (system templates) is treated
        // distinct in PostgreSQL — acceptable for v3.
        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_NacRoles_TenantId_NormalizedName");
    }
}
