using Nac.Identity.Permissions.Cache;

namespace Nac.Identity.Tests.Infrastructure;

/// <summary>
/// <see cref="IPermissionGrantCache"/> test double that records invalidation calls so
/// tests can assert which keys/patterns were touched. Delegates reads to the factory
/// on each call (no caching side-effect).
/// </summary>
public sealed class RecordingPermissionGrantCache : IPermissionGrantCache
{
    public List<string> Invalidated { get; } = [];
    public List<string> InvalidatedPatterns { get; } = [];

    public Task<HashSet<string>> GetOrLoadAsync(string cacheKey,
        Func<CancellationToken, Task<HashSet<string>>> factory,
        TimeSpan ttl, CancellationToken ct = default) => factory(ct);

    public Task InvalidateAsync(string cacheKey, CancellationToken ct = default)
    {
        Invalidated.Add(cacheKey);
        return Task.CompletedTask;
    }

    public Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default)
    {
        InvalidatedPatterns.Add(pattern);
        return Task.CompletedTask;
    }
}
