namespace Nac.Core.Domain;

public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken ct = default) where TId : notnull;
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(Specification<T> spec, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Specification<T> spec, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(Specification<T> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(Specification<T> spec, CancellationToken ct = default);
}

public interface IRepository<T> : IReadRepository<T> where T : class
{
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
