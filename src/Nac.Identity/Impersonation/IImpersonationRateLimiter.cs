namespace Nac.Identity.Impersonation;

/// <summary>
/// Atomic rate-limiter for impersonation token issuance. Hard limit 10 tokens / 5 min
/// per host user. Primary backend: Redis <c>INCR</c>+<c>EXPIRE</c>. Fallback: DB count.
/// </summary>
public interface IImpersonationRateLimiter
{
    /// <summary>
    /// Attempts to consume one token slot for <paramref name="hostUserId"/>. Returns
    /// <c>false</c> when the 5-min window is saturated. MUST be atomic under concurrency.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid hostUserId, CancellationToken ct = default);
}
