using Nac.Core.Domain;

namespace Nac.Testing.Fakes;

public class FakeRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items = [];
    public IReadOnlyList<T> Items => _items.AsReadOnly();

    // Operations log
    public List<T> AddedItems { get; } = [];
    public List<T> UpdatedItems { get; } = [];
    public List<T> DeletedItems { get; } = [];

    public Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        _items.Add(entity);
        AddedItems.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        UpdatedItems.Add(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _items.Remove(entity);
        DeletedItems.Add(entity);
        return Task.CompletedTask;
    }

    public Task<T?> GetByIdAsync<TId>(TId id, CancellationToken ct = default) where TId : notnull
    {
        // Convention: entity must have an "Id" property matching TId
        var item = _items.FirstOrDefault(e =>
        {
            var prop = typeof(T).GetProperty("Id");
            return prop is not null && Equals(prop.GetValue(e), id);
        });
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<T>>(_items.AsReadOnly());

    public Task<IReadOnlyList<T>> ListAsync(Specification<T> spec, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<T>>(
            _items.Where(spec.IsSatisfiedBy).ToList().AsReadOnly());

    public Task<T?> FirstOrDefaultAsync(Specification<T> spec, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(spec.IsSatisfiedBy));

    public Task<int> CountAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.Count);

    public Task<int> CountAsync(Specification<T> spec, CancellationToken ct = default) =>
        Task.FromResult(_items.Count(spec.IsSatisfiedBy));

    public Task<bool> AnyAsync(Specification<T> spec, CancellationToken ct = default) =>
        Task.FromResult(_items.Any(spec.IsSatisfiedBy));

    public FakeRepository<T> WithItems(params T[] items)
    {
        _items.AddRange(items);
        return this;
    }
}
