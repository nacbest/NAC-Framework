using Microsoft.EntityFrameworkCore;

namespace Nac.Persistence.Extensions;

/// <summary>
/// Configuration options for <c>AddNacPersistence&lt;TContext&gt;</c>.
/// Consumers call <see cref="UseDbContext"/> to supply the provider-specific
/// setup (e.g. <c>UseSqlServer</c>, <c>UseNpgsql</c>), and the <c>Enable*</c>
/// methods to opt in to individual interceptors and the outbox pattern.
/// </summary>
public sealed class NacPersistenceOptions
{
    /// <summary>
    /// Gets the action that configures the underlying <see cref="DbContextOptionsBuilder"/>.
    /// Set via <see cref="UseDbContext"/>.
    /// </summary>
    internal Action<DbContextOptionsBuilder>? ConfigureDbContext { get; private set; }

    /// <summary>Gets a value indicating whether the audit interceptor is enabled.</summary>
    internal bool AuditEnabled { get; private set; }

    /// <summary>Gets a value indicating whether the soft-delete interceptor is enabled.</summary>
    internal bool SoftDeleteEnabled { get; private set; }

    /// <summary>Gets a value indicating whether the domain-event interceptor is enabled.</summary>
    internal bool DomainEventEnabled { get; private set; }

    /// <summary>Gets a value indicating whether the outbox interceptor and worker are enabled.</summary>
    internal bool OutboxEnabled { get; private set; }

    /// <summary>
    /// Registers a callback to configure the <see cref="DbContextOptionsBuilder"/> with a
    /// concrete database provider.
    /// </summary>
    /// <param name="configure">
    /// An action that receives the <see cref="DbContextOptionsBuilder"/> and applies
    /// provider-specific options (connection string, retry policy, etc.).
    /// </param>
    /// <returns>The same <see cref="NacPersistenceOptions"/> instance for chaining.</returns>
    public NacPersistenceOptions UseDbContext(Action<DbContextOptionsBuilder> configure)
    {
        ConfigureDbContext = configure;
        return this;
    }

    /// <summary>
    /// Enables the <c>AuditableEntityInterceptor</c> which auto-populates
    /// <c>CreatedAt</c>, <c>CreatedBy</c>, and <c>UpdatedAt</c> on entities implementing
    /// <c>IAuditableEntity</c>.
    /// </summary>
    /// <returns>The same <see cref="NacPersistenceOptions"/> instance for chaining.</returns>
    public NacPersistenceOptions EnableAuditInterceptor()
    {
        AuditEnabled = true;
        return this;
    }

    /// <summary>
    /// Enables the <c>SoftDeleteInterceptor</c> which converts hard-deletes into soft-deletes
    /// for entities implementing <c>ISoftDeletable</c>.
    /// </summary>
    /// <returns>The same <see cref="NacPersistenceOptions"/> instance for chaining.</returns>
    public NacPersistenceOptions EnableSoftDeleteInterceptor()
    {
        SoftDeleteEnabled = true;
        return this;
    }

    /// <summary>
    /// Enables the <c>DomainEventInterceptor</c> which dispatches domain events via
    /// <c>IDomainEventDispatcher</c> after each successful save.
    /// </summary>
    /// <returns>The same <see cref="NacPersistenceOptions"/> instance for chaining.</returns>
    public NacPersistenceOptions EnableDomainEventInterceptor()
    {
        DomainEventEnabled = true;
        return this;
    }

    /// <summary>
    /// Enables the transactional outbox pattern: the <c>OutboxInterceptor</c> serialises
    /// integration events into the outbox table within the same transaction, and the
    /// <c>OutboxWorker</c> background service polls and publishes them.
    /// </summary>
    /// <returns>The same <see cref="NacPersistenceOptions"/> instance for chaining.</returns>
    public NacPersistenceOptions EnableOutbox()
    {
        OutboxEnabled = true;
        return this;
    }
}
