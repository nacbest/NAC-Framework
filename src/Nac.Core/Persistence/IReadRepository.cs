namespace Nac.Core.Persistence;

/// <summary>
/// Read-only repository. Returns completed result sets — never IQueryable.
/// Complex queries should use <see cref="ISpecification{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type to query.</typeparam>
public interface IReadRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken ct = default) where TId : notnull;
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
}
