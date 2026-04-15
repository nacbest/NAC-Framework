namespace Nac.Domain.Persistence;

/// <summary>
/// Unit of Work abstraction. Manages transaction lifecycle for a single DbContext.
/// The UnitOfWork pipeline behavior calls <see cref="SaveChangesAsync"/> automatically
/// after the command handler completes — handlers should NOT call SaveChanges directly.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
