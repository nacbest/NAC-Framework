using Nac.Core.Primitives;

namespace Nac.Identity.Impersonation;

/// <summary>
/// Raised when an <see cref="ImpersonationSession"/> transitions to revoked. Consumed
/// by the outbox dispatcher (phase 07) to fan out a cross-service signal; the in-process
/// blacklist (phase 04) is updated synchronously by the service layer.
/// </summary>
public sealed record ImpersonationRevokedEvent(
    Guid SessionId,
    string Jti,
    Guid HostUserId,
    string TenantId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
