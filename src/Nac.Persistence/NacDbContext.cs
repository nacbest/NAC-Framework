using Microsoft.EntityFrameworkCore;
using Nac.Core.Auth;
using Nac.Core.Messaging;
using Nac.Domain;
using Nac.Persistence.Conventions;
using Nac.Persistence.Outbox;

namespace Nac.Persistence;

/// <summary>
/// Base DbContext for all NAC module contexts. Provides:
/// <list type="bullet">
///   <item>Automatic audit field population (CreatedAt/By, LastModifiedAt/By)</item>
///   <item>Soft-delete conversion (Deleted state becomes Modified + IsDeleted flag)</item>
///   <item>Domain event collection from tracked aggregate roots</item>
///   <item>Outbox table for integration event persistence</item>
///   <item>Global query filter for <see cref="ISoftDeletable"/> via convention</item>
/// </list>
/// </summary>
public abstract class NacDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    /// <summary>Integration events pending dispatch by the Outbox background worker.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Processed integration events for idempotency/deduplication on the consumer side.</summary>
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected NacDbContext(DbContextOptions options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new SoftDeleteQueryFilterConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Sync SaveChanges is blocked because it bypasses audit and soft-delete processing.
    /// Always use <see cref="SaveChangesAsync"/> instead.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw new NotSupportedException(
            "Use SaveChangesAsync. Synchronous SaveChanges bypasses audit and soft-delete processing.");

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser?.UserId;

        ConvertSoftDeletes(now, userId);
        ApplyAuditFields(now, userId);

        return await base.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Collects all pending domain events from tracked entities and clears them.
    /// Called by <see cref="UnitOfWork.UnitOfWorkBehavior{TCommand,TResponse}"/>
    /// after a successful <see cref="SaveChangesAsync"/>.
    /// </summary>
    public IReadOnlyList<INotification> CollectAndClearDomainEvents()
    {
        var entries = ChangeTracker.Entries<IHasDomainEvents>().ToList();

        var events = entries
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        entries.ForEach(e => e.Entity.ClearDomainEvents());

        return events;
    }

    private void ApplyAuditFields(DateTimeOffset now, string? userId)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy ??= userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = now;
                entry.Entity.LastModifiedBy = userId;
            }
        }
    }

    /// <summary>
    /// Converts Delete state to Modified + soft-delete flags.
    /// Runs BEFORE ApplyAuditFields so the resulting Modified state triggers LastModifiedAt.
    /// </summary>
    private void ConvertSoftDeletes(DateTimeOffset now, string? userId)
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedBy = userId;
        }
    }
}
