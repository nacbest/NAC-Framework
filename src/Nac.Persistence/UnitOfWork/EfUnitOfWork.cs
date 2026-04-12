using Microsoft.EntityFrameworkCore.Storage;
using Nac.Abstractions.Messaging;

namespace Nac.Persistence.UnitOfWork;

/// <summary>
/// EF Core implementation of <see cref="INacUnitOfWork"/>.
/// Wraps a <see cref="NacDbContext"/> to provide transactional save and domain event collection.
/// Does NOT own the DbContext — DI container manages the context's lifetime.
/// </summary>
public sealed class EfUnitOfWork<TContext> : INacUnitOfWork
    where TContext : NacDbContext
{
    private readonly TContext _context;
    private IDbContextTransaction? _transaction;

    public EfUnitOfWork(TContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<INotification> CollectAndClearDomainEvents()
        => _context.CollectAndClearDomainEvents();

    // Only dispose the transaction we own — DbContext is managed by DI container
    public void Dispose() => _transaction?.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();
    }
}
