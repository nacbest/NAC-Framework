namespace Nac.Identity.Impersonation;

/// <summary>
/// Orchestration layer for host→tenant impersonation. Caller MUST be an authenticated
/// host user with the <c>Host.ImpersonateTenant</c> permission. See phase 05 spec.
/// </summary>
public interface IImpersonationService
{
    /// <summary>
    /// Issues a 15-minute impersonation token for <paramref name="tenantId"/> on behalf
    /// of <paramref name="hostUserId"/> (which MUST equal the current user id — self-only).
    /// Persists the audit session in the same call.
    /// </summary>
    Task<ImpersonationIssueResult> IssueAsync(
        Guid hostUserId, string tenantId, string reason, CancellationToken ct = default);

    /// <summary>
    /// Revokes an active session. Adds the <c>jti</c> to the blacklist and flips
    /// <c>RevokedAt</c> on the aggregate. Idempotent.
    /// </summary>
    Task RevokeAsync(Guid sessionId, Guid callerUserId, CancellationToken ct = default);

    /// <summary>Paged listing of sessions scoped to a tenant (most recent first).</summary>
    Task<IReadOnlyList<ImpersonationSession>> ListByTenantAsync(
        string tenantId, int skip, int take, CancellationToken ct = default);
}
