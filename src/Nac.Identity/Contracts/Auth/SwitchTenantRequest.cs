namespace Nac.Identity.Contracts.Auth;

/// <summary>
/// Payload for <c>POST /auth/switch-tenant</c>.
/// </summary>
public sealed record SwitchTenantRequest(string TenantId);
