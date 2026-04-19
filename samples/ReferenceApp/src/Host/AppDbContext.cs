using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;

namespace ReferenceApp.Host;

/// <summary>
/// Host-owned identity DbContext. Inherits all Identity table mappings
/// (NacUsers, NacRoles, NacUserRoles, etc.) from <see cref="NacIdentityDbContext"/>.
/// Places Identity tables in the "identity" schema.
/// </summary>
public sealed class AppDbContext : NacIdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        base.OnModelCreating(modelBuilder);
    }
}
