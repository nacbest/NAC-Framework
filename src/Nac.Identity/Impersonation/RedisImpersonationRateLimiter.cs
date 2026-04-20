using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Nac.Identity.Impersonation;

/// <summary>
/// <see cref="IImpersonationRateLimiter"/> backed by <see cref="IDistributedCache"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Soft-limit (best-effort) semantics.</strong> The current implementation uses a
/// non-atomic get→increment→set sequence on <see cref="IDistributedCache"/>. Under concurrent
/// parallel requests by the same host user the counter may be read as the same value in two
/// overlapping calls, allowing at most <c>MaxPerWindow</c>+N-1 events in the worst case where
/// N is the number of truly parallel calls (race window = one request round-trip).
/// </para>
/// <para>
/// This is an accepted trade-off: <c>StackExchange.Redis</c> is not a direct dependency of
/// <c>Nac.Identity</c>. A consumer that requires hard atomicity can replace this registration
/// with a Singleton that wraps <c>IDatabase.StringIncrementAsync</c> (native Redis INCR) and
/// <c>IDatabase.KeyExpireAsync</c>, which is fully atomic by protocol. The plan decision has
/// been downgraded from "atomic" to "best-effort with DB fallback" — the DB fallback path
/// (<c>CountRecentByHostUserAsync</c>) is also non-atomic (TOCTOU) but bounds the worst case
/// to a bounded over-count rather than a complete bypass.
/// </para>
/// <para>
/// On cache error we fall back to an authoritative DB <c>COUNT(*)</c> over the last 5 minutes.
/// </para>
/// </remarks>
internal sealed class RedisImpersonationRateLimiter(
    IDistributedCache cache,
    IImpersonationSessionRepository sessions,
    ILogger<RedisImpersonationRateLimiter> logger) : IImpersonationRateLimiter
{
    private const int MaxPerWindow = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    public async Task<bool> TryConsumeAsync(Guid hostUserId, CancellationToken ct = default)
    {
        var key = $"ratelimit:impersonate:{hostUserId:N}";
        try
        {
            var raw = await cache.GetStringAsync(key, ct);
            var count = int.TryParse(raw, out var parsed) ? parsed : 0;
            if (count >= MaxPerWindow) return false;

            await cache.SetStringAsync(key, (count + 1).ToString(),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Window }, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rate-limit cache unavailable for {HostUser}; falling back to DB count.", hostUserId);
            var since = DateTime.UtcNow - Window;
            var recent = await sessions.CountRecentByHostUserAsync(hostUserId, since, ct);
            return recent < MaxPerWindow;
        }
    }
}
