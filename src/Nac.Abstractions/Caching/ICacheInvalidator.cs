namespace Nac.Abstractions.Caching;

/// <summary>
/// Marker interface for commands that invalidate cache entries.
/// Implement on commands to declare which cache keys should be evicted
/// after the command handler completes successfully.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>Cache keys to invalidate after successful command execution.</summary>
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
