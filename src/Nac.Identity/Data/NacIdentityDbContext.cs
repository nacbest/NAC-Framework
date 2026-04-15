using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nac.Identity.Entities;

namespace Nac.Identity.Data;

/// <summary>
/// Generic Identity DbContext for NAC Framework.
/// Supports derived user types: class AppDbContext : NacIdentityDbContext&lt;AppUser&gt;
/// </summary>
public class NacIdentityDbContext<TUser> : IdentityDbContext<TUser, NacRole, Guid>
    where TUser : NacIdentityUser
{
    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Accepts non-generic DbContextOptions to allow derived classes to pass their own typed options.
    /// </summary>
    public NacIdentityDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new Configurations.TenantRoleConfiguration());
        builder.ApplyConfiguration(new Configurations.TenantMembershipConfiguration());
        builder.ApplyConfiguration(new Configurations.RefreshTokenConfiguration());
    }
}

/// <summary>
/// Non-generic convenience alias using <see cref="NacIdentityUser"/> directly.
/// Use when you don't need a custom user type.
/// </summary>
public class NacIdentityDbContext : NacIdentityDbContext<NacIdentityUser>
{
    public NacIdentityDbContext(DbContextOptions<NacIdentityDbContext> options) : base(options)
    {
    }
}
