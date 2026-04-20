using Nac.Identity.Impersonation;

namespace Nac.Identity.Management.Contracts.Impersonation;

/// <summary>
/// Public projection of <see cref="ImpersonationSession"/> for the admin list endpoint.
/// <c>Jti</c> is intentionally excluded — it is an internal revocation key and must not
/// be exposed via the API (exposure would allow bearer replay attacks on the blacklist).
/// </summary>
public sealed record ImpersonationSessionDto(
    Guid Id,
    Guid HostUserId,
    string TenantId,
    string Reason,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt)
{
    /// <summary>Maps a domain aggregate to its safe public DTO.</summary>
    public static ImpersonationSessionDto From(ImpersonationSession session) => new(
        session.Id,
        session.HostUserId,
        session.TenantId,
        session.Reason,
        session.IssuedAt,
        session.ExpiresAt,
        session.RevokedAt);
}
