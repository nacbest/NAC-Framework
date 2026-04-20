using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Primitives;

namespace Nac.Persistence.Interceptors;

/// <summary>
/// EF Core save-changes interceptor that automatically populates audit fields
/// (<see cref="IAuditableEntity.CreatedAt"/>, <see cref="IAuditableEntity.CreatedBy"/>,
/// <see cref="IAuditableEntity.UpdatedAt"/>) before each <c>SaveChangesAsync</c> call.
/// </summary>
internal sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="AuditableEntityInterceptor"/>.
    /// </summary>
    /// <param name="serviceProvider">
    /// Used to optionally resolve <see cref="ICurrentUser"/>; no exception is thrown if
    /// the service is not registered.
    /// </param>
    /// <param name="dateTimeProvider">Provides the current UTC time.</param>
    public AuditableEntityInterceptor(IServiceProvider serviceProvider, IDateTimeProvider dateTimeProvider)
    {
        _serviceProvider = serviceProvider;
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
        var currentUser = _serviceProvider.GetService<ICurrentUser>();
        var userId = currentUser?.Id.ToString();
        var impersonatorId = currentUser?.ImpersonatorId?.ToString();

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
                entry.Entity.ImpersonatorId = impersonatorId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
                entry.Entity.ImpersonatorId = impersonatorId;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
