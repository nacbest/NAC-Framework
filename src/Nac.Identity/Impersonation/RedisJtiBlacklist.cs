using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Nac.Identity.Impersonation;

/// <summary>
/// <see cref="IJtiBlacklist"/> backed by <see cref="IDistributedCache"/>. Production
/// deployments register a Redis-backed cache; dev uses in-memory. Key namespace is
/// <c>impersonation:revoked:{jti}</c> — isolated from other cache consumers.
/// </summary>
internal sealed class RedisJtiBlacklist(IDistributedCache cache, ILogger<RedisJtiBlacklist> logger)
    : IJtiBlacklist
{
    private const string KeyPrefix = "impersonation:revoked:";

    public async Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);
        // 60s buffer: absorbs clock skew between issuer and cache host.
        var options = new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt.AddSeconds(60) };
        await cache.SetStringAsync(KeyPrefix + jti, "1", options, ct);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jti)) return true; // fail-closed: no jti = untrusted
        try
        {
            var value = await cache.GetStringAsync(KeyPrefix + jti, ct);
            return value is not null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JTI blacklist cache error for {Jti}; failing closed (deny).", jti);
            return true;
        }
    }
}
