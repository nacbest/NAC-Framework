using Nac.Core.Primitives;

namespace Nac.Identity.Impersonation;

/// <summary>
/// Append-only audit aggregate for a single host→tenant impersonation grant. Persisted
/// via <c>NacIdentityDbContext</c>. Lifecycle: <see cref="Issue"/> → (optional) <see cref="Revoke"/>.
/// No soft-delete: revocation flips <see cref="RevokedAt"/> but the row persists forever.
/// </summary>
public sealed class ImpersonationSession : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>Host user that minted the impersonation token.</summary>
    public Guid HostUserId { get; private set; }

    /// <summary>Target tenant slug the session is scoped to.</summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>Operator-supplied justification (10–500 chars, validated at API layer).</summary>
    public string Reason { get; private set; } = default!;

    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Non-null once <see cref="Revoke"/> succeeded.</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>JWT <c>jti</c> — unique, unguessable (128-bit). Primary revocation lookup key.</summary>
    public string Jti { get; private set; } = default!;

    // ── IAuditableEntity ─────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? ImpersonatorId { get; set; }

    /// <summary>EF Core constructor — never call from application code.</summary>
    private ImpersonationSession() { }

    /// <summary>
    /// Factory for a brand-new session. Caller is the service layer that already
    /// validated authorization + reason. <paramref name="jti"/> MUST be unguessable.
    /// </summary>
    public static ImpersonationSession Issue(
        Guid hostUserId, string tenantId, string reason, string jti, TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        var now = DateTime.UtcNow;
        return new ImpersonationSession
        {
            Id = Guid.NewGuid(),
            HostUserId = hostUserId,
            TenantId = tenantId,
            Reason = reason,
            Jti = jti,
            IssuedAt = now,
            ExpiresAt = now.Add(ttl),
        };
    }

    /// <summary>
    /// Idempotent revocation. Re-revoke is a no-op; the domain event is raised exactly
    /// once (on the first successful transition).
    /// </summary>
    public void Revoke(DateTime now)
    {
        if (RevokedAt is not null) return;
        RevokedAt = now;
        AddDomainEvent(new ImpersonationRevokedEvent(Id, Jti, HostUserId, TenantId));
    }
}
