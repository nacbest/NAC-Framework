using Microsoft.EntityFrameworkCore;
using Nac.Core.Persistence;

namespace Nac.Persistence.Repository;

/// <summary>
/// EF Core implementation of <see cref="IRepository{TEntity}"/>.
/// Does NOT call SaveChanges — changes are tracked by the DbContext and flushed
/// by <see cref="UnitOfWork.UnitOfWorkBehavior{TCommand,TResponse}"/> after the handler returns.
/// </summary>
public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public EfRepository(DbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken ct = default)
        where TId : notnull
        => DbSet.FindAsync([id], ct).AsTask();

    /// <inheritdoc/>
    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => SpecificationEvaluator.Evaluate(DbSet, spec).FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator.Evaluate(DbSet, spec).ToListAsync(ct);

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken ct = default)
        => DbSet.CountAsync(ct);

    /// <inheritdoc/>
    public Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => SpecificationEvaluator.Evaluate(DbSet, spec).CountAsync(ct);

    /// <inheritdoc/>
    public Task<bool> AnyAsync(CancellationToken ct = default)
        => DbSet.AnyAsync(ct);

    /// <inheritdoc/>
    public Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => SpecificationEvaluator.Evaluate(DbSet, spec).AnyAsync(ct);

    /// <inheritdoc/>
    public void Add(TEntity entity) => DbSet.Add(entity);

    /// <inheritdoc/>
    public void AddRange(IEnumerable<TEntity> entities) => DbSet.AddRange(entities);

    /// <inheritdoc/>
    public void Update(TEntity entity) => DbSet.Update(entity);

    /// <inheritdoc/>
    public void Remove(TEntity entity) => DbSet.Remove(entity);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<TEntity> entities) => DbSet.RemoveRange(entities);
}
