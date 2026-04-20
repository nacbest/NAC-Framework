using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nac.Identity.Impersonation;
using Nac.Identity.Memberships;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Users;
using Nac.Persistence.Context;

namespace Nac.Identity.Context;

/// <summary>
/// Abstract EF Core <see cref="DbContext"/> for Identity-based NAC applications.
/// Inherits soft-delete query filters from <see cref="NacDbContext"/> and maps all
/// ASP.NET Core Identity tables to prefixed NAC table names. Pattern A: users are
/// global (no <c>TenantId</c>); tenant access is expressed via
/// <see cref="UserTenantMembership"/> and permissions via <see cref="PermissionGrant"/>.
/// </summary>
public abstract class NacIdentityDbContext : NacDbContext
{
    /// <summary>Users stored in the identity store.</summary>
    public DbSet<NacUser> Users => Set<NacUser>();

    /// <summary>Roles stored in the identity store.</summary>
    public DbSet<NacRole> Roles => Set<NacRole>();

    /// <summary>User ↔ tenant membership rows.</summary>
    public DbSet<UserTenantMembership> Memberships => Set<UserTenantMembership>();

    /// <summary>Role assignments within a membership.</summary>
    public DbSet<MembershipRole> MembershipRoles => Set<MembershipRole>();

    /// <summary>Flat permission grant table (ABP-style, provider-based).</summary>
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    /// <summary>Host → tenant impersonation audit sessions.</summary>
    public DbSet<ImpersonationSession> ImpersonationSessions => Set<ImpersonationSession>();

    /// <inheritdoc cref="NacDbContext(DbContextOptions)"/>
    protected NacIdentityDbContext(DbContextOptions options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applies soft-delete filters and configurations from the concrete context's assembly.
        base.OnModelCreating(modelBuilder);

        // Always apply configurations shipped with Nac.Identity even when the concrete
        // DbContext lives in a consumer assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NacIdentityDbContext).Assembly);

        ConfigureIdentityTables(modelBuilder);
    }

    /// <summary>
    /// Maps ASP.NET Core Identity entity types to NAC-prefixed table names and applies
    /// the minimal column constraints for <see cref="NacUser"/> not covered by a
    /// dedicated <c>IEntityTypeConfiguration</c> class.
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

            b.Property(u => u.FullName).HasMaxLength(200);
            b.HasIndex(u => u.Email).IsUnique();
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
    }
}
