namespace Nac.Identity.Jwt;

/// <summary>
/// Re-issues a tenant-scoped JWT for a user after validating an Active membership.
/// </summary>
public interface ITenantSwitchService
{
    /// <summary>
    /// Produces a new token scoped to <paramref name="tenantId"/> for <paramref name="userId"/>.
    /// Throws when the user has no Active membership in the tenant.
    /// </summary>
    Task<TenantSwitchResult> IssueTokenForTenantAsync(Guid userId, string tenantId,
                                                     CancellationToken ct = default);
}

/// <summary>Output of a successful tenant switch.</summary>
public sealed record TenantSwitchResult(string AccessToken, IReadOnlyList<Guid> RoleIds,
                                         DateTime ExpiresAt);
