using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nac.Identity.Entities;

namespace Nac.Identity.Data;

/// <summary>
/// Identity DbContext for NAC Framework.
/// Manages users, roles, tenant memberships, and refresh tokens.
/// </summary>
public class NacIdentityDbContext : IdentityDbContext<NacUser, NacRole, Guid>
{
    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public NacIdentityDbContext(DbContextOptions<NacIdentityDbContext> options)
        : base(options)
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
