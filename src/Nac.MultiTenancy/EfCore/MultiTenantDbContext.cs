using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Domain;
using Nac.Core.Primitives;
using Nac.MultiTenancy.Abstractions;
using Nac.Persistence.Context;

namespace Nac.MultiTenancy.EfCore;

/// <summary>
/// Abstract <see cref="DbContext"/> that composes per-tenant query filters with
/// soft-delete filters from <see cref="NacDbContext"/>. EF Core only allows one
/// query filter per entity, so this class builds a combined expression:
/// <c>e.TenantId == currentTenantId [&amp;&amp; !e.IsDeleted]</c>.
/// </summary>
public abstract class MultiTenantDbContext : NacDbContext
{
    private readonly ITenantContext _tenantContext;

    protected MultiTenantDbContext(DbContextOptions options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base to apply assembly configs. Soft-delete filters from base
        // are intentionally overridden below with composed filters.
        base.OnModelCreating(modelBuilder);
        ApplyTenantAndSoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// Builds a single composed query filter per entity that combines:
    /// - Tenant filter (for ITenantEntity)
    /// - Soft-delete filter (for ISoftDeletable)
    /// EF Core's HasQueryFilter replaces any prior filter, so we must compose
    /// both conditions into one expression to avoid losing the soft-delete filter.
    /// </summary>
    private void ApplyTenantAndSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isTenant = typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType);
            if (!isTenant) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");

            // Tenant filter: e.TenantId == _tenantContext.TenantId
            var tenantIdProp = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var contextConstant = Expression.Constant(_tenantContext, typeof(ITenantContext));
            var currentTenantId = Expression.Property(contextConstant, nameof(ITenantContext.TenantId));
            Expression condition = Expression.Equal(tenantIdProp, currentTenantId);

            // If entity also implements ISoftDeletable, compose: && !e.IsDeleted
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var isDeletedProp = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = Expression.Equal(isDeletedProp, Expression.Constant(false));
                condition = Expression.AndAlso(condition, notDeleted);
            }

            var lambda = Expression.Lambda(condition, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
