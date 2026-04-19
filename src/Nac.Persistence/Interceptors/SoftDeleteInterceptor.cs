using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nac.Core.Abstractions;
using Nac.Core.Primitives;

namespace Nac.Persistence.Interceptors;

/// <summary>
/// EF Core save-changes interceptor that converts hard-deletes into soft-deletes for any
/// entity implementing <see cref="ISoftDeletable"/>.
/// Entities with <see cref="EntityState.Deleted"/> are switched to <see cref="EntityState.Modified"/>
/// and have their <see cref="ISoftDeletable.IsDeleted"/> and <see cref="ISoftDeletable.DeletedAt"/>
/// properties set before the underlying <c>SaveChangesAsync</c> executes.
/// </summary>
internal sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="SoftDeleteInterceptor"/>.
    /// </summary>
    /// <param name="dateTimeProvider">Provides the current UTC time.</param>
    public SoftDeleteInterceptor(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
