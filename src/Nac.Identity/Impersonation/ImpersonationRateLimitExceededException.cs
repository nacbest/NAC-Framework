namespace Nac.Identity.Impersonation;

/// <summary>
/// Thrown by <see cref="IImpersonationService.IssueAsync"/> when the per-host-user
/// rate-limit (10 tokens / 5 min) is exceeded. Controller maps to HTTP 429 with
/// <c>Retry-After: 300</c>.
/// </summary>
public sealed class ImpersonationRateLimitExceededException()
    : Exception("Impersonation token rate limit exceeded (10 tokens per 5 minutes).");
