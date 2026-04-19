using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nac.Core.Domain;
using Nac.MultiTenancy.Abstractions;

namespace Nac.MultiTenancy.EfCore;

/// <summary>
/// EF Core save-changes interceptor that automatically stamps the current
/// <see cref="ITenantContext.TenantId"/> onto every newly <see cref="EntityState.Added"/>
/// entity implementing <see cref="ITenantEntity"/>.
/// Throws <see cref="InvalidOperationException"/> when a tenant entity is being inserted
/// without an active tenant context, preventing accidental cross-tenant data writes.
/// </summary>
internal sealed class TenantEntityInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampTenantId(eventData);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampTenantId(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampTenantId(DbContextEventData eventData)
    {
        if (eventData.Context is null) return;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                entry.Entity.TenantId = tenantContext.TenantId
                    ?? throw new InvalidOperationException(
                        "No tenant context available for new tenant entity. " +
                        "Ensure the tenant resolution middleware has run before persisting data.");
            }
        }
    }
}
