using Microsoft.EntityFrameworkCore;
using Nac.Core.Domain;
using Nac.Persistence.Context;

namespace Nac.Persistence.Repository;

/// <summary>
/// Default EF Core implementation of <see cref="IRepository{T}"/> and <see cref="IReadRepository{T}"/>.
/// Registered as an open-generic service via DI — consumers never reference this type directly.
/// </summary>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
internal sealed class Repository<T> : IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initialises a new instance of <see cref="Repository{T}"/>.
    /// </summary>
    /// <param name="context">The active <see cref="NacDbContext"/> scoped to the current request.</param>
    public Repository(NacDbContext context)
    {
        _dbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken ct = default) where TId : notnull =>
        await _dbSet.FindAsync([id], ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await _dbSet.ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> ListAsync(Specification<T> spec, CancellationToken ct = default) =>
        await _dbSet.Where(spec.ToExpression()).ToListAsync(ct);

    /// <inheritdoc />
    public async Task<T?> FirstOrDefaultAsync(Specification<T> spec, CancellationToken ct = default) =>
        await _dbSet.Where(spec.ToExpression()).FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _dbSet.CountAsync(ct);

    /// <inheritdoc />
    public async Task<int> CountAsync(Specification<T> spec, CancellationToken ct = default) =>
        await _dbSet.Where(spec.ToExpression()).CountAsync(ct);

    /// <inheritdoc />
    public async Task<bool> AnyAsync(Specification<T> spec, CancellationToken ct = default) =>
        await _dbSet.Where(spec.ToExpression()).AnyAsync(ct);

    /// <inheritdoc />
    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        var entry = await _dbSet.AddAsync(entity, ct);
        return entry.Entity;
    }

    /// <inheritdoc />
    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
