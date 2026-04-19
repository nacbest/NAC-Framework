using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nac.Identity.Users;
using Nac.Persistence.Context;

namespace Nac.Identity.Context;

/// <summary>
/// Abstract EF Core <see cref="DbContext"/> for Identity-based NAC applications.
/// Inherits soft-delete query filters from <see cref="NacDbContext"/> and maps all
/// ASP.NET Core Identity tables to prefixed NAC table names.
/// </summary>
public abstract class NacIdentityDbContext : NacDbContext
{
    /// <summary>Users stored in the identity store.</summary>
    public DbSet<NacUser> Users => Set<NacUser>();

    /// <summary>Roles stored in the identity store.</summary>
    public DbSet<NacRole> Roles => Set<NacRole>();

    /// <inheritdoc cref="NacDbContext(DbContextOptions)"/>
    protected NacIdentityDbContext(DbContextOptions options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applies soft-delete filters and assembly configurations from NacDbContext.
        base.OnModelCreating(modelBuilder);

        ConfigureIdentityTables(modelBuilder);
    }

    /// <summary>
    /// Maps all Identity entity types to NAC-prefixed table names and applies
    /// column constraints for <see cref="NacUser"/> and <see cref="NacRole"/>.
    /// </summary>
    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        // ── Table names + keys ───────────────────────────────────────────────
        // Since we extend NacDbContext (not IdentityDbContext), we must manually
        // configure all composite keys and relationships that Identity requires.

        modelBuilder.Entity<NacUser>(b =>
        {
            b.ToTable("NacUsers");
            b.HasKey(u => u.Id);
            b.HasMany<IdentityUserClaim<Guid>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            b.HasMany<IdentityUserLogin<Guid>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
            b.HasMany<IdentityUserToken<Guid>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
            b.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
        });

        modelBuilder.Entity<NacRole>(b =>
        {
            b.ToTable("NacRoles");
            b.HasKey(r => r.Id);
            b.HasMany<IdentityRoleClaim<Guid>>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
            b.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(b =>
        {
            b.ToTable("NacUserRoles");
            b.HasKey(r => new { r.UserId, r.RoleId });
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("NacUserClaims");
            b.HasKey(uc => uc.Id);
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b =>
        {
            b.ToTable("NacRoleClaims");
            b.HasKey(rc => rc.Id);
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("NacUserLogins");
            b.HasKey(l => new { l.LoginProvider, l.ProviderKey });
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("NacUserTokens");
            b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
        });

        // ── NacUser constraints ───────────────────────────────────────────────

        modelBuilder.Entity<NacUser>(user =>
        {
            user.Property(u => u.FullName)
                .HasMaxLength(200);

            user.Property(u => u.TenantId)
                .HasMaxLength(64)
                .IsRequired();

            // Unique index on Email (already non-null in IdentityUser).
            user.HasIndex(u => u.Email)
                .IsUnique();

            // Index for efficient tenant-scoped queries.
            user.HasIndex(u => u.TenantId);
        });

        // ── NacRole constraints ───────────────────────────────────────────────

        modelBuilder.Entity<NacRole>(role =>
        {
            role.Property(r => r.Description)
                .HasMaxLength(500);
        });
    }
}
