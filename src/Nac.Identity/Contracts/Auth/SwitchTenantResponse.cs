namespace Nac.Identity.Contracts.Auth;

/// <summary>
/// Response for <c>POST /auth/switch-tenant</c>. The new token contains
/// <c>tenant_id</c> and <c>role_ids</c> claims.
/// </summary>
public sealed record SwitchTenantResponse(
    string AccessToken,
    IReadOnlyList<Guid> RoleIds,
    DateTime ExpiresAt);
