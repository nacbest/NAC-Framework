using Nac.Caching;

namespace Nac.Testing.Fakes;

public sealed class FakeNacCache : INacCache
{
    private readonly Dictionary<string, object?> _store = [];

    // Operations log
    public List<string> Gets { get; } = [];
    public List<(string Key, object? Value)> Sets { get; } = [];
    public List<string> Removes { get; } = [];
    public List<string> TagRemovals { get; } = [];

    public ValueTask<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        Gets.Add(key);
        if (_store.TryGetValue(key, out var cached))
            return new ValueTask<T>((T)cached!);

        return StoreAndReturn(key, factory(ct));
    }

    private async ValueTask<T> StoreAndReturn<T>(string key, ValueTask<T> task)
    {
        var value = await task;
        _store[key] = value;
        Sets.Add((key, value));
        return value;
    }

    public ValueTask SetAsync<T>(
        string key, T value,
        CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        _store[key] = value;
        Sets.Add((key, value));
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken ct = default)
    {
        _store.Remove(key);
        Removes.Add(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
        TagRemovals.Add(tag);
        return ValueTask.CompletedTask;
    }
}
