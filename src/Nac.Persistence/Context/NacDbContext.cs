using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Abstractions;
using Nac.Core.Primitives;

namespace Nac.Persistence.Context;

/// <summary>
/// Abstract base <see cref="DbContext"/> for all NAC Framework persistence contexts.
/// Implements <see cref="IUnitOfWork"/>, auto-applies entity configurations from the
/// concrete context's assembly, and registers a global soft-delete query filter for
/// any entity that implements <see cref="ISoftDeletable"/>.
/// </summary>
public abstract class NacDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// Initialises a new instance of <see cref="NacDbContext"/>.
    /// </summary>
    /// <param name="options">The options to be used by the context.</param>
    protected NacDbContext(DbContextOptions options) : base(options) { }

    /// <inheritdoc />
    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct) =>
        base.SaveChangesAsync(ct);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discover IEntityTypeConfiguration<T> implementations from the concrete context assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        // Apply global query filter to exclude soft-deleted rows.
        ApplySoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// Iterates all entity types and applies <c>IsDeleted == false</c> query filters
    /// for any type implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
