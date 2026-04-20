namespace Nac.Identity.Impersonation;

/// <summary>
/// Revocation blacklist for impersonation JWT <c>jti</c> values. Only impersonation tokens
/// are tracked — regular bearer tokens skip the check. Entries self-expire at the token's
/// <c>ExpiresAt</c>, so no cleanup job is required.
/// </summary>
public interface IJtiBlacklist
{
    /// <summary>Marks <paramref name="jti"/> revoked; the entry evicts at <paramref name="expiresAt"/>.</summary>
    Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> if the token is revoked. Fail-closed: on cache error the method
    /// returns <c>true</c> (deny), trading availability for security.
    /// </summary>
    Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default);
}
