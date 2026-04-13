namespace Nac.Abstractions.Persistence;

/// <summary>
/// Write repository for entities. Does not expose IQueryable.
/// Mutations are tracked by the DbContext and persisted when UnitOfWork commits.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by this repository.</typeparam>
public interface IRepository<TEntity> : IReadRepository<TEntity> where TEntity : class
{
    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);
}
