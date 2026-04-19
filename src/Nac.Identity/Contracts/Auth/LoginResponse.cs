namespace Nac.Identity.Contracts.Auth;

/// <summary>
/// Response for <c>POST /auth/login</c>. The returned token is tenantless;
/// the client selects a tenant and calls <c>POST /auth/switch-tenant</c>.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    LoginUserInfo User,
    IReadOnlyList<MembershipListItem> Memberships);

/// <summary>Basic user info embedded in <see cref="LoginResponse"/>.</summary>
public sealed record LoginUserInfo(
    Guid Id,
    string Email,
    string? FullName,
    bool IsHost);
