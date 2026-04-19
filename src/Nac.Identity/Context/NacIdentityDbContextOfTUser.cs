using Microsoft.EntityFrameworkCore;
using Nac.Identity.Users;

namespace Nac.Identity.Context;

/// <summary>
/// Generic variant of <see cref="NacIdentityDbContext"/> that allows downstream
/// applications to substitute a custom user type derived from <see cref="NacUser"/>
/// while retaining all Identity table mappings and soft-delete filters.
/// </summary>
/// <typeparam name="TUser">A concrete user type that extends <see cref="NacUser"/>.</typeparam>
public abstract class NacIdentityDbContext<TUser> : NacIdentityDbContext
    where TUser : NacUser
{
    /// <summary>
    /// Typed <see cref="DbSet{TEntity}"/> for <typeparamref name="TUser"/>, shadowing
    /// the base <c>Users</c> property to return the concrete set.
    /// </summary>
    public new DbSet<TUser> Users => Set<TUser>();

    /// <inheritdoc cref="NacIdentityDbContext(DbContextOptions)"/>
    protected NacIdentityDbContext(DbContextOptions options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
